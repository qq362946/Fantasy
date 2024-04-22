#if FANTASY_UNITY
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fantasy.Core;
using Fantasy.Unity;
using UnityEditor;
using UnityEngine;

namespace Fantasy
{
    public class AssetBundleComponentAwakeSystem : AwakeSystem<AssetBundleComponent>
    {
        protected override void Awake(AssetBundleComponent self)
        {
            var selfScene = self.Scene;
            self.AssetBundleLock = selfScene.CoroutineLockComponent.Create(self.GetType().TypeHandle.Value.ToInt64());
        }
    }
    
    public class AssetBundleComponent : Entity
    {
        public  ulong NeedUpdateSize;
        public AssetBundleManifest AssetBundleManifestObject;
        public readonly Dictionary<string, string[]> DependenciesCache = new ();
        public readonly Dictionary<string, string> AssetBundlePathCache = new ();
        public readonly Dictionary<string, AssetBundleInfo> AssetBundles = new ();
        public readonly OneToManyDictionary<string, string, UnityEngine.Object> Resources = new ();
        public CoroutineLockQueueType AssetBundleLock;

        #region Initialize

        /// <summary>
        /// AssetBundle初始化
        /// </summary>
        public async FTask Initialize()
        {
            Log.Debug($"热更资源存放路径:{AppDefine.RemoteAssetBundlePath}");
            
            try
            {
                // 安装包安装后首次运行时会检测StreamingAssets里的文件、如果有就拷贝到Persistent中
                
                if (File.Exists(AppDefine.StreamingAssetsVersion) && !File.Exists(AppDefine.PersistentDataVersion))
                {
                    var versionBytes = await GetStreamingAssets(AppDefine.VersionName);
                    var assetBundleVersionInfo = ProtoBuffHelper.FromBytes<AssetBundleVersionInfo>(versionBytes);
            
                    foreach (var assetBundleVersion in assetBundleVersionInfo.List)
                    {
                        // 获得persistentDataPath和StreamingAssets的路径
                        var assetBundle = $"{AppDefine.RemoteAssetBundlePath}/{assetBundleVersion.Name}";
                        // 在persistentDataPath创建文件夹
                        CreateDirectory(assetBundleVersion.Name, UnityEngine.Application.persistentDataPath, true);
                        // 拷贝文件到目标路径中
                        var bytes = await GetStreamingAssets(assetBundleVersion.Name);
                        if (bytes is { Length: > 0 })
                        {
                            await File.WriteAllBytesAsync(assetBundle, bytes);
                        }
                    }
                
                    await File.WriteAllBytesAsync(AppDefine.PersistentDataVersion, versionBytes);
                    // 更新下AssetBundleManifest
                    UpdateAssetBundleManifest();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        #endregion

        #region StartUpdate

        /// <summary>
        /// 更新AssetBundle
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<AssetBundleCheckStage> StartUpdate(Download download)
        {
            var assetBundleUpdate = new AssetBundleUpdate(this, download);

            yield return AssetBundleCheckStage.CheckVersionMD5;

            switch (await assetBundleUpdate.CheckVersionMD5())
            {
                case AssetBundleUpdateState.NoUpdateRequired:
                {
                    UpdateAssetBundleManifest();
                    yield return AssetBundleCheckStage.Complete;
                    yield break;
                }
                case AssetBundleUpdateState.ConnectFailed:
                {
                    yield return AssetBundleCheckStage.UpdateFailed;
                    yield break;
                }
            }
            
            yield return AssetBundleCheckStage.DownloadVersion;

            if (await assetBundleUpdate.DownloadVersion() == AssetBundleUpdateState.ConnectFailed)
            {
                yield return AssetBundleCheckStage.UpdateFailed;
                yield break;
            }
            
            yield return AssetBundleCheckStage.DownloadAssetBundle;
            
            if (await assetBundleUpdate.DownloadAssetBundle() == AssetBundleUpdateState.ConnectFailed)
            {
                yield return AssetBundleCheckStage.UpdateFailed;
                yield break;
            }

            UpdateAssetBundleManifest();
            yield return AssetBundleCheckStage.Complete;
        }

        #endregion

        #region LoadBundle

        public void LoadBundle(string assetBundleName)
        {
            var collectDependencies = CollectDependencies(assetBundleName);

            foreach (var dependency in collectDependencies)
            {
                if (string.IsNullOrEmpty(dependency))
                {
                    continue;
                }

                LoadOneBundle(dependency);
            }
        }
        
        public async FTask LoadBundleAsync(string assetBundleName)
        {
            var collectDependencies = CollectDependencies(assetBundleName);

            if (collectDependencies.Count > 0)
            {
                using var tasks = ReuseList<FTask>.Create();
                tasks.AddRange(collectDependencies.Select(LoadOneBundleAsync));
                await FTask.WhenAll(tasks);
            }
        }
        
        private void LoadOneBundle(string assetBundleName)
        {
            if (AssetBundles.TryGetValue(assetBundleName, out var assetBundleInfo))
            {
                assetBundleInfo.RefCount++;
                return;
            }

            if (AppDefine.IsEditor)
            {
#if UNITY_EDITOR || UNITY_EDITOR_64
                AddResourceInEditor(assetBundleName);
#endif
                return;
            }

            var assetBundlePath = ToAssetBundlePath(assetBundleName);

            if (!File.Exists(assetBundlePath))
            {
                Log.Error($"AssetBundle not found:{assetBundleName} {assetBundlePath}");
                return;
            }

            var assetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            
            if (assetBundle == null)
            {
                Debug.Log($"Unable to load AssetBundle: {assetBundleName} {assetBundlePath}");
                return;
            }

            if (!assetBundle.isStreamedSceneAssetBundle)
            {
                var assets = assetBundle.LoadAllAssets();
                
                foreach (var asset in assets)
                {
                    AddResource(assetBundleName, asset.name, asset);
                }
            }

            AssetBundles[assetBundleName] = AssetBundleInfo.Create(Scene, assetBundleName, assetBundle);
        }
        
        private async FTask LoadOneBundleAsync(string assetBundleName)
        {
            using (await AssetBundleLock.Lock(assetBundleName.GetHashCode()))
            {
                if (AssetBundles.TryGetValue(assetBundleName, out var assetBundleInfo))
                {
                    assetBundleInfo.RefCount++;
                    return;
                }

                if (AppDefine.IsEditor)
                {
#if UNITY_EDITOR || UNITY_EDITOR_64
                    AddResourceInEditor(assetBundleName);
                    // 放到下一帧执行、这样编辑器下也不会是同步加载了
                    await Scene.TimerComponent.Core.WaitFrameAsync();
#endif
                    return;
                }
                
                var assetBundlePath = ToAssetBundlePath(assetBundleName);

                if (!File.Exists(assetBundlePath))
                {
                    Log.Error($"AssetBundle not found:{assetBundleName} {assetBundlePath}");
                    return;
                }

                var assetBundle = await LoadFromFileAsync(assetBundlePath);
                
                if (assetBundle == null)
                {
                    Debug.Log($"Unable to load AssetBundle: {assetBundleName} {assetBundlePath}");
                    return;
                }
                
                if (!assetBundle.isStreamedSceneAssetBundle)
                {
                    var assets = assetBundle.LoadAllAssets();
                    
                    foreach (var asset in assets)
                    {
                        AddResource(assetBundleName, asset.name, asset);
                    }
                }

                AssetBundles[assetBundleName] = AssetBundleInfo.Create(Scene, assetBundleName, assetBundle);
            }
        }

        public AssetBundle GetAssetBundle(string bundleName)
        {
            return AssetBundles.TryGetValue(bundleName, out var assetBundleInfo) ? assetBundleInfo.AssetBundle : null;
        }

        #endregion

        #region UnloadBundle
        
        // unload = true 卸载从 AssetBundle 加载的所有游戏对象（及其依赖项）。
        // 这不包括复制的游戏对象（例如实例化的游戏对象），因为它们不再属于 AssetBundle。发生这种情况时，
        // 从该 AssetBundle 加载的纹理（并且仍然属于它）会从场景中的游戏对象消失，因此 Unity 将它们视为缺少纹理
        // unload = false 那么将会中断 M 和 AB 当前实例的链接关系 ，不会销毁现有的对象引用
        // 所以通常都是unload = true ， 除非是场景或一些特殊的包 需要卸载后不会销毁已经引用的资源才会使用unload = false
        // 详细可以看 https://zhuanlan.zhihu.com/p/483821413

        public void UnloadBundle(string assetBundleName, bool unload = true)
        {
            var collectDependencies = CollectDependencies(assetBundleName);

            foreach (var dependency in collectDependencies)
            {
                UnloadOneBundle(dependency, unload);
            }
        }

        public async FTask UnloadBundleAsync(string assetBundleName, bool unload = true)
        {
            var collectDependencies = CollectDependencies(assetBundleName);

            foreach (var dependency in collectDependencies)
            {
                using (await AssetBundleLock.Lock(dependency.GetHashCode()))
                {
                    await UnloadOneBundleAsync(dependency, unload);
                }
            }
        }

        private void UnloadOneBundle(string assetBundleName, bool unload)
        {
            if (!AssetBundles.TryGetValue(assetBundleName, out var assetBundleInfo))
            {
                return;
            }

            if (--assetBundleInfo.RefCount > 0)
            {
                return;
            }

            AssetBundles.Remove(assetBundleName);
            Resources.Remove(assetBundleName);
            assetBundleInfo.Destroy(unload);
            // Log.Debug($"UnloadOneBundle assetBundleName:{assetBundleName} AssetBundles:{AssetBundles.Count} Resources:{Resources.Count}");
        }

        private async FTask UnloadOneBundleAsync(string assetBundleName, bool unload)
        {
            if (!AssetBundles.TryGetValue(assetBundleName, out var assetBundleInfo))
            {
                return;
            }

            if (--assetBundleInfo.RefCount > 0)
            {
                return;
            }

            AssetBundles.Remove(assetBundleName);
            Resources.Remove(assetBundleName);
            await assetBundleInfo.DestroyAsync(unload);
        }

        #endregion

        #region GetAsset

        public T GetAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
        {
            return (T)GetAsset(bundleName, assetName);
        }

        public UnityEngine.Object GetAsset(string bundleName, string assetName)
        {
            if (!Resources.TryGetValue(bundleName, assetName, out var resources))
            {
                throw new Exception($"not found asset: {bundleName} {assetName}");
            }

            return resources;
        }

        #endregion

        #region Resource

        private void AddResource(string assetBundleName, string assetName, UnityEngine.Object resource)
        {
            Resources.Add(assetBundleName, assetName, resource);
        }
#if UNITY_EDITOR || UNITY_EDITOR_64
        private void AddResourceInEditor(string assetBundleName)
        {
            var assetPathsFromAssetBundle = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);

            if (assetPathsFromAssetBundle.Length > 0)
            {
                foreach (var assetPath in assetPathsFromAssetBundle)
                {
                    var assetName = Path.GetFileNameWithoutExtension(assetPath);
                    var resource = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    AddResource(assetBundleName, assetName, resource);
                }

                AssetBundles[assetBundleName] = AssetBundleInfo.Create(Scene, assetBundleName);
                return;
            }

            Log.Error($"assets bundle not found:{assetBundleName}");
        }
#endif
        #endregion

        #region Dependencie

        private HashSet<string> CollectDependencies(string assetBundleName, HashSet<string> depends = null)
        {
            depends ??= new HashSet<string>();
            depends.Add(assetBundleName);

            var dependencies = GetDependencies(assetBundleName);

            if (dependencies.Length <= 0)
            {
                return depends;
            }

            foreach (var dependency in dependencies)
            {
                if (dependency == assetBundleName)
                {
                    throw new Exception($"包有循环依赖，请重新标记: {assetBundleName} {dependency}");
                }

                CollectDependencies(dependency, depends);
            }

            return depends;
        }

        private string[] GetDependencies(string assetBundleName)
        {
            if (DependenciesCache.TryGetValue(assetBundleName, out var assetBundleDependencies))
            {
                return assetBundleDependencies;
            }

            if (AppDefine.IsEditor)
            {
#if UNITY_EDITOR || UNITY_EDITOR_64
                assetBundleDependencies = AssetDatabase.GetAssetBundleDependencies(assetBundleName, true);
#endif
            }
            else
            {
                assetBundleDependencies = AssetBundleManifestObject.GetAllDependencies(assetBundleName);
            }
            
            DependenciesCache[assetBundleName] = assetBundleDependencies;
            return assetBundleDependencies;
        }

        #endregion

        #region Other

        private void UpdateAssetBundleManifest()
        {
            if (!AppDefine.IsEditor)
            {
                LoadOneBundle(AppDefine.AssetBundleManifestName);
                AssetBundleManifestObject = GetAsset<AssetBundleManifest>(AppDefine.AssetBundleManifestName, "AssetBundleManifest");
                UnloadBundle(AppDefine.AssetBundleManifestName, false);
            }
        }

        private async FTask<byte[]> GetStreamingAssets(string path)
        {
            // 在Android平台上，由于StreamingAssets文件夹下的文件是压缩的，需要使用Unity的WWW类进行读取。
            // 在其他平台上，可以直接使用System.IO类进行读取。
            var filePath = $"{UnityEngine.Application.streamingAssetsPath}/{path}";
#if UNITY_ANDROID
            return await Download.Instance.DownloadByte($"file://{filePath}");
#else
            return await File.ReadAllBytesAsync(filePath);
#endif
        }
        
        public void CreateDirectory(string filePath, string targetDirectory, bool isFile)
        {
            var directories = filePath.Split('/');

            if (directories.Length <= 1)
            {
                return;
            }

            var dir = "";
            var forLength = isFile ? directories.Length - 1 : directories.Length;

            for (var i = 0; i < forLength; i++)
            {
                dir = $"{dir}{directories[i]}/";
                var destinationDirectory = $"{targetDirectory}/{dir}";

                if (Directory.Exists(destinationDirectory))
                {
                    continue;
                }

                Directory.CreateDirectory(destinationDirectory);
            }
        }
        
        public byte[] CalculateMD5(string directoryPath, bool includeManifest)
        {
            var substrNameIndex = directoryPath.Length + 1;
            var assetBundleVersionInfoList = new AssetBundleVersionInfo();
            var sourceFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

            foreach (var filePath in sourceFiles)
            {
                if ((!includeManifest && filePath.EndsWith(".manifest")) || filePath.EndsWith(".meta") || filePath.EndsWith(".DS_Store"))
                {
                    File.Delete(filePath);
                    continue;
                }

                var assetBundleVersion = GetAssetBundleVersion(filePath, substrNameIndex);
                assetBundleVersionInfoList.Add(assetBundleVersion);
            }

            return ProtoBuffHelper.ToBytes(assetBundleVersionInfoList);
        }
        
        private AssetBundleVersion GetAssetBundleVersion(string filePath, int substrNameIndex)
        {
            var fileInfo = new FileInfo(filePath);
            using var file = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            return new AssetBundleVersion
            {
                MD5 = EncryptHelper.FileMD5(file),
                Size = (ulong)file.Length,
                Name = filePath.Substring(substrNameIndex)
            };
        }
        
        public string ToAssetBundlePath(string bundleName)
        {
            if (AssetBundlePathCache.TryGetValue(bundleName, out var assetBundlePath))
            {
                return assetBundlePath;
            }

            assetBundlePath = $"{AppDefine.RemoteAssetBundlePath}/{bundleName}";
            AssetBundlePathCache[assetBundlePath] = assetBundlePath;
            return assetBundlePath;
        }

        private FTask<AssetBundle> LoadFromFileAsync(string assetBundlePath)
        {
            var task = FTask<AssetBundle>.Create();
            var request = AssetBundle.LoadFromFileAsync(assetBundlePath);
            request.completed += operation => { task.SetResult(request.assetBundle); };
            return task;
        }
        
        public string GetPlatform()
        {
            switch (UnityEngine.Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                {
                    return "StandaloneOSX";
                }
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                {
                    return "StandaloneWindows64";
                }
                case RuntimePlatform.IPhonePlayer:
                {
                    return "iOS";
                }
                case RuntimePlatform.Android:
                {
                    return "Android";
                }
                case RuntimePlatform.WebGLPlayer:
                {
                    return "WebGL";
                }
                default:
                {
                    throw new NotSupportedException($"NotSupported platform:{UnityEngine.Application.platform}");
                }
            }
        }

        #endregion
    }
}
#endif