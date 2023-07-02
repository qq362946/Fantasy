// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using Fantasy.Core;
// using Fantasy.Helper;
// using UnityEngine;
//
// namespace Fantasy
// {
//     public sealed class UpdateAssetBundle : MonoBehaviour, IDisposable
//     {
//         private static UpdateAssetBundle _instance;
//         private static GameObject _updateAssetBundleGameObject;
//
//         private string _remotePath;
//         private string _platformName;
//         private string _localVersionMD5;
//         private string _localVersionName;
//         private AssetBundleVersionInfo _localVersionInfo;
//         private readonly HashSet<AssetBundleVersion> _updateFiles = new();
//
//         public static UpdateAssetBundle Instance
//         {
//             get
//             {
//                 if (_instance != null)
//                 {
//                     return _instance;
//                 }
//                 
//                 _updateAssetBundleGameObject = new GameObject("UpdateAssetBundle");
//                 _instance = _updateAssetBundleGameObject.AddComponent<UpdateAssetBundle>();
//                 return _instance;
//             }
//         }
//         
//         public void Dispose()
//         {
//             if (_instance == null)
//             {
//                 return;
//             }
//             
//             GameObject.Destroy(_updateAssetBundleGameObject);
//         }
//         
//         public async FTask<bool> StartAsync()
//         {
//             Log.Debug($"热更资源存放路径:{Define.RemoteAssetBundlePath}");
//             
//             _platformName = GetPlatform();
//             _localVersionName = $"{Define.RemoteAssetBundlePath}/{Define.VersionName}";
//             _localVersionMD5 = $"{Define.RemoteAssetBundlePath}/{Define.VersionMD5Name}";
//             var hotfixResDir = Directory.GetDirectories(Define.RemoteAssetBundlePath, "*", SearchOption.AllDirectories);
//
//             // 删除掉多余的文件夹
//             
//             foreach (var directory in hotfixResDir)
//             {
//                 var subDirInfo = new DirectoryInfo(directory);
//                 
//                 if (subDirInfo.GetFiles().Length == 0)
//                 {
//                     Directory.Delete(directory);
//                 }
//             }
//
//             return await DownloadAssetBundle();
//         }
//
//         private async FTask<bool> DownloadAssetBundle()
//         {
//             _remotePath = $"{Define.RemoteUpdatePath}{_platformName}";
//             
//             // 1、下载远程的VersionMD5文件、对比MD5来确定是否有可更新的资源
//
//             var remoteVersionMD5Url = $"{_remotePath}/{Define.VersionMD5Name}";
//             var remoteVersionMD5 = await Download.Instance.DownloadText(remoteVersionMD5Url);
//
//             if (remoteVersionMD5 == null)
//             {
//                 return false;
//             }
//             
//             if (File.Exists(_localVersionMD5))
//             {
//                 var localVersionMD5Text = await File.ReadAllTextAsync(_localVersionMD5);
//
//                 if (localVersionMD5Text == remoteVersionMD5)
//                 {
//                     Log.Debug("The client's resources are already up to date and do not need to be updated");
//                     // 对比MD5发现一致、不需要下载任何东西到客户端
//                     return true;
//                 }
//             }
//             
//             // 有部分可能会放到StreamingAssets里打进包里的、这里就不处理了、因为这样更新就会出现双份了
//             // 如果实在有这样的需求的话、自己改下代码支持下
//             // 其实就是检查StreamingAssets跟远程的是否一致、如果不一致就放到外部文件夹一份
//             
//             // 2、下载VersionName来对比需要更新的资源列表
//             
//             var remoteVersionName = await Download.Instance.DownloadByte($"{_remotePath}/{Define.VersionName}");
//             
//             if (remoteVersionName == null)
//             {
//                 return false;
//             }
//             
//             _updateFiles.Clear();
//             
//             ulong updateSize = 0;
//             var deleteFiles = new HashSet<AssetBundleVersion>();
//             var remoteVersionInfo = ProtoBufHelper.FromBytes<AssetBundleVersionInfo>(remoteVersionName);
//
//             if (File.Exists(_localVersionName))
//             {
//                 var localVersionNameBytes = await File.ReadAllBytesAsync(_localVersionName);
//                 _localVersionInfo = ProtoBufHelper.FromBytes<AssetBundleVersionInfo>(localVersionNameBytes);
//                 
//                 remoteVersionInfo.Initialize();
//                 
//                 foreach (var assetBundleVersion in _localVersionInfo.List)
//                 {
//                     if (!remoteVersionInfo.Dic.TryGetValue(assetBundleVersion.Name, out var remoteAssetBundleVersion))
//                     {
//                         deleteFiles.Add(assetBundleVersion);
//                         continue;
//                     }
//
//                     if (remoteAssetBundleVersion.MD5 == assetBundleVersion.MD5)
//                     {
//                         continue;
//                     }
//
//                     updateSize += remoteAssetBundleVersion.Size;
//                     remoteVersionInfo.Remove(remoteAssetBundleVersion);
//                     _updateFiles.Add(remoteAssetBundleVersion);
//                 }
//
//                 foreach (var deleteFile in deleteFiles)
//                 {
//                     File.Delete(deleteFile.Name);
//                     _localVersionInfo.Remove(deleteFile);
//                 }
//             }
//             else
//             {
//                 _localVersionInfo = new AssetBundleVersionInfo();
//             }
//             
//             foreach (var assetBundleVersion in remoteVersionInfo.List)
//             {
//                 updateSize += assetBundleVersion.Size;
//                 _updateFiles.Add(assetBundleVersion);
//             }
//             
//             //3、下载需要更新的资源
//
//             var task = new List<FTask>();
//             
//             foreach (var assetBundleVersion in _updateFiles.ToArray())
//             {
//                 task.Add(DownloadAssetBundle(assetBundleVersion));
//             }
//
//             await FTask.WhenAll(task);
//
//             if (_updateFiles.Count > 0)
//             {
//                 return false;
//             }
//             
//             //4、写入文件清单
//             
//             await File.WriteAllTextAsync(_localVersionMD5,remoteVersionMD5);
//             await File.WriteAllBytesAsync(_localVersionName, ProtoBufHelper.ToBytes(_localVersionInfo));
//             return true;
//         }
//
//         private async FTask DownloadAssetBundle(AssetBundleVersion assetBundleVersion)
//         {
//             var downloadByte = await Download.Instance.DownloadByte($"{_remotePath}/{assetBundleVersion.Name}");
//
//             if (downloadByte == null || downloadByte.Length == 0)
//             {
//                 Log.Error($"DownloadAssetBundle fail AssetBundle:{assetBundleVersion.Name}");
//                 return;
//             }
//             
//             // 保存下载的包
//             
//             var savePath = $"{Define.RemoteAssetBundlePath}/{assetBundleVersion.Name}";
//             var saveDirectory = assetBundleVersion.Name.Split('/');
//             var currentDirectory = Define.RemoteAssetBundlePath;
//
//             if (saveDirectory.Length > 1)
//             {
//                 for (var i = 0; i < saveDirectory.Length - 1; i++)
//                 {
//                     currentDirectory += $"/{saveDirectory[i]}";
//
//                     if (!Directory.Exists(currentDirectory))
//                     {
//                         Directory.CreateDirectory(currentDirectory);
//                     }
//                 }
//             }
//             
//             await File.WriteAllBytesAsync(savePath, downloadByte);
//             
//             ThreadSynchronizationContext.Main.Post(() =>
//             {
//                 _localVersionInfo[name] = assetBundleVersion;
//                 _updateFiles.Remove(assetBundleVersion);
//             });
//         }
//
//         private static string GetPlatform()
//         {
//             switch (UnityEngine.Application.platform)
//             {
//                 case RuntimePlatform.OSXEditor:
//                 case RuntimePlatform.OSXPlayer:
//                 {
//                     return "StandaloneOSX";
//                 }
//                 case RuntimePlatform.WindowsEditor:
//                 case RuntimePlatform.WindowsPlayer:
//                 {
//                     return "StandaloneWindows64";
//                 }
//                 case RuntimePlatform.IPhonePlayer:
//                 {
//                     return "iOS";
//                 }
//                 case RuntimePlatform.Android:
//                 {
//                     return "Android";
//                 }
//                 case RuntimePlatform.WebGLPlayer:
//                 {
//                     return "WebGL";
//                 }
//                 default:
//                 {
//                     throw new NotSupportedException($"NotSupported platform:{UnityEngine.Application.platform}");
//                 }
//             }
//         }
//
//         private void OnDestroy()
//         {
//             _instance = null;
//             _updateAssetBundleGameObject = null;
//         }
//     }
// }