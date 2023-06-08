#if FANTASY_UNITY
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fantasy.Core;
using Fantasy.DataStructure;
using UnityEditor;
using UnityEngine;

namespace Fantasy.Helper
{
    public static class AssetBundleHelper
    {
        // AssetBundle
        private static readonly AssetBundleManifest AssetBundleManifestObject;
        private static readonly Dictionary<string, string[]> DependenciesCache = new ();
        private static readonly Dictionary<string, string> AssetBundlePathCache = new ();
        private static readonly Dictionary<string, AssetBundleInfo> AssetBundles = new ();
        private static readonly OneToManyDictionary<string, string, UnityEngine.Object> Resources = new ();
        // 加载AB资源包协程锁
        private static readonly CoroutineLockQueueType AssetBundleLock = new CoroutineLockQueueType("AssetBundleLock");

        static AssetBundleHelper()
        {
#if !UNITY_EDITOR
            LoadOneBundle(Define.AssetBundleManifestName);
            AssetBundleManifestObject = GetAsset<AssetBundleManifest>(Define.AssetBundleManifestName, "AssetBundleManifest");
            UnloadBundle(Define.AssetBundleManifestName, false);
#endif
        }

        #region LoadBundle

        public static void LoadBundle(string assetBundleName)
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
        
        public static async FTask LoadBundleAsync(string assetBundleName)
        {
            var collectDependencies = CollectDependencies(assetBundleName);

            if (collectDependencies.Count > 0)
            {
                using var tasks = ReuseList<FTask>.Create();
                tasks.AddRange(collectDependencies.Select(LoadOneBundleAsync));
                await FTask.WhenAll(tasks);
            }
        }
        
        private static void LoadOneBundle(string assetBundleName)
        {
            if (AssetBundles.TryGetValue(assetBundleName, out var assetBundleInfo))
            {
                assetBundleInfo.RefCount++;
                return;
            }

            if (Define.IsEditor)
            {
#if UNITY_EDITOR || UNITY_EDITOR_64
                AddResourceInEditor(assetBundleName);
#endif
                return;
            }

            var assetBundlePath = assetBundleName.ToAssetBundlePath();

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
            
            AssetBundles[assetBundleName] = AssetBundleInfo.Create(assetBundleName, assetBundle);
        }
        
        private static async FTask LoadOneBundleAsync(string assetBundleName)
        {
            using (await AssetBundleLock.Lock(assetBundleName.GetHashCode()))
            {
                if (AssetBundles.TryGetValue(assetBundleName, out var assetBundleInfo))
                {
                    assetBundleInfo.RefCount++;
                    return;
                }

                if (Define.IsEditor)
                {
#if UNITY_EDITOR || UNITY_EDITOR_64
                    AddResourceInEditor(assetBundleName);
                    // 放到下一帧执行、这样编辑器下也不会是同步加载了
                    await TimerScheduler.Instance.Core.WaitFrameAsync();
#endif
                    return;
                }
                
                var assetBundlePath = assetBundleName.ToAssetBundlePath();

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
            
                AssetBundles[assetBundleName] = AssetBundleInfo.Create(assetBundleName, assetBundle);
            }
        }

        public static AssetBundle GetAssetBundle(string bundleName)
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

        public static void UnloadBundle(string assetBundleName, bool unload = true)
        {
            var collectDependencies = CollectDependencies(assetBundleName);

            foreach (var dependency in collectDependencies)
            {
                UnloadOneBundle(dependency, unload);
            }
        }

        public static async FTask UnloadBundleAsync(string assetBundleName, bool unload = true)
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

        private static void UnloadOneBundle(string assetBundleName, bool unload)
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

        private static async FTask UnloadOneBundleAsync(string assetBundleName, bool unload)
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

        public static T GetAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
        {
            return (T)GetAsset(bundleName, assetName);
        }

        public static UnityEngine.Object GetAsset(string bundleName, string assetName)
        {
            if (!Resources.TryGetValue(bundleName, assetName, out var resources))
            {
                throw new Exception($"not found asset: {bundleName} {assetName}");
            }

            return resources;
        }

        #endregion

        #region Resource

        private static void AddResource(string assetBundleName, string assetName, UnityEngine.Object resource)
        {
            Resources.Add(assetBundleName, assetName, resource);
        }
#if UNITY_EDITOR || UNITY_EDITOR_64
        private static void AddResourceInEditor(string assetBundleName)
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

                AssetBundles[assetBundleName] = AssetBundleInfo.Create(assetBundleName);
                return;
            }

            Log.Error($"assets bundle not found:{assetBundleName}");
        }
#endif
        #endregion

        #region Dependencie

        private static HashSet<string> CollectDependencies(string assetBundleName, HashSet<string> depends = null)
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

        private static string[] GetDependencies(string assetBundleName)
        {
            if (DependenciesCache.TryGetValue(assetBundleName, out var assetBundleDependencies))
            {
                return assetBundleDependencies;
            }

            if (Define.IsEditor)
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

        public static string ToAssetBundlePath(this string bundleName)
        {
            if (AssetBundlePathCache.TryGetValue(bundleName, out var assetBundlePath))
            {
                return assetBundlePath;
            }

            assetBundlePath = $"{Define.AppHotfixResPath}/{bundleName}";
            AssetBundlePathCache[assetBundlePath] = assetBundlePath;
            return assetBundlePath;
        }

        private static FTask<AssetBundle> LoadFromFileAsync(string assetBundlePath)
        {
            var task = FTask<AssetBundle>.Create();
            var request = AssetBundle.LoadFromFileAsync(assetBundlePath);
            request.completed += operation => { task.SetResult(request.assetBundle); };
            return task;
        }

        #endregion
    }
}
#endif