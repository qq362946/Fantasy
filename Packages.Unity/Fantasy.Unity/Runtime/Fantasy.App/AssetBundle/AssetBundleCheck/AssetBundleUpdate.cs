using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
// ReSharper disable MethodHasAsyncOverload

namespace Fantasy
{
    public enum AssetBundleUpdateState
    {
        /// <summary>
        /// 操作完成
        /// </summary>
        Complete = 1,
        /// <summary>
        /// 不需要更新
        /// </summary>
        NoUpdateRequired = 2,
        /// <summary>
        /// 连接远程服务器失败
        /// </summary>
        ConnectFailed = 3,
    }
    
    public class AssetBundleUpdate
    {
        private string _remotePath;
        private readonly string _platformName;
        private readonly string _localVersionMD5;
        private readonly string _localVersionName;
        private string _remoteVersionMD5;

        private Download _download;
        private AssetBundleVersionInfo _localVersionInfo;
        private readonly HashSet<AssetBundleVersion> _updateFiles = new();

        public AssetBundleUpdate(Download download)
        {
            _download = download;
            _platformName = AssetBundleHelper.GetPlatform();
            _localVersionMD5 = $"{Define.RemoteAssetBundlePath}/{Define.VersionMD5Name}";
            _localVersionName = $"{Define.RemoteAssetBundlePath}/{Define.VersionName}";
        }

        public async FTask<AssetBundleUpdateState> CheckVersionMD5()
        {
            _download.Clear();
            _remotePath = $"{Define.RemoteUpdatePath}{_platformName}";

            try
            {
                var remoteVersionMD5Url = $"{_remotePath}/{Define.VersionMD5Name}";
                Log.Debug(remoteVersionMD5Url);
                _remoteVersionMD5 = await _download.DownloadText(remoteVersionMD5Url);
            
                if (_remoteVersionMD5 == null)
                {
                    // 执行到这里一般都是无法访问到目标服务器导致的、但也有可能是目标服务器没有这个文件
                    // 一般不会出现这样的低级错误吧NotFoundVersionMD5
                    Log.Error($"Not Found VersionMD5");
                    return AssetBundleUpdateState.ConnectFailed;
                }

                if (!File.Exists(_localVersionMD5))
                {
                    return AssetBundleUpdateState.Complete;
                }
                
                var localVersionMD5Text = await File.ReadAllTextAsync(_localVersionMD5);
                return localVersionMD5Text == _remoteVersionMD5 ? AssetBundleUpdateState.NoUpdateRequired : AssetBundleUpdateState.Complete;
            }
            catch (Exception e)
            {
                Log.Error(e);
                return AssetBundleUpdateState.ConnectFailed;
            }
        }

        public async FTask<AssetBundleUpdateState> DownloadVersion()
        {
            try
            {
                _download.Clear();
                var remoteVersionName = await _download.DownloadByte($"{_remotePath}/{Define.VersionName}");

                if (remoteVersionName == null)
                {
                    Log.Error($"Not Found VersionName");
                    return AssetBundleUpdateState.ConnectFailed;
                }

                _updateFiles.Clear();
                AssetBundleHelper.NeedUpdateSize = 0;
                var deleteFiles = new HashSet<AssetBundleVersion>();
                var remoteVersionInfo = ProtoBufHelper.FromBytes<AssetBundleVersionInfo>(remoteVersionName);

                if (File.Exists(_localVersionName))
                {
                    var localVersionNameBytes = await File.ReadAllBytesAsync(_localVersionName);
                    _localVersionInfo = ProtoBufHelper.FromBytes<AssetBundleVersionInfo>(localVersionNameBytes);

                    remoteVersionInfo.Initialize();

                    foreach (var assetBundleVersion in _localVersionInfo.List)
                    {
                        if (!remoteVersionInfo.Dic.TryGetValue(assetBundleVersion.Name, out var remoteAssetBundleVersion))
                        {
                            deleteFiles.Add(assetBundleVersion);
                            continue;
                        }

                        if (remoteAssetBundleVersion.MD5 == assetBundleVersion.MD5)
                        {
                            continue;
                        }

                        AssetBundleHelper.NeedUpdateSize += remoteAssetBundleVersion.Size;
                        remoteVersionInfo.Remove(remoteAssetBundleVersion);
                        _updateFiles.Add(remoteAssetBundleVersion);
                    }

                    foreach (var deleteFile in deleteFiles)
                    {
                        File.Delete(deleteFile.Name);
                        _localVersionInfo.Remove(deleteFile);
                    }
                }
                else
                {
                    _localVersionInfo = new AssetBundleVersionInfo();
                }

                foreach (var assetBundleVersion in remoteVersionInfo.List)
                {
                    AssetBundleHelper.NeedUpdateSize += assetBundleVersion.Size;
                    _updateFiles.Add(assetBundleVersion);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                return AssetBundleUpdateState.ConnectFailed;
            }
            
            return AssetBundleUpdateState.Complete;
        }

        public async FTask<AssetBundleUpdateState> DownloadAssetBundle()
        {
            try
            {
                _download.Clear();
                
                if (!_updateFiles.Any())
                {
                    return AssetBundleUpdateState.Complete;
                }
                
                var task = _updateFiles.ToArray().Select(DownloadAssetBundle).ToList();

                await FTask.WhenAll(task);
                
                if (_updateFiles.Count > 0)
                {
                    foreach (var assetBundleVersion in _updateFiles)
                    {
                        Log.Error($"AssetBundle name: {assetBundleVersion.Name} cannot be updated normally");
                    }
                    
                    return AssetBundleUpdateState.ConnectFailed;
                }

                AssetBundleHelper.NeedUpdateSize = 0;
                File.WriteAllText(_localVersionMD5, _remoteVersionMD5);
                File.WriteAllBytes(_localVersionName, ProtoBufHelper.ToBytes(_localVersionInfo));
                return AssetBundleUpdateState.Complete;
            }
            catch (Exception e)
            {
                Log.Error(e);
                return AssetBundleUpdateState.ConnectFailed;;
            }
        }

        private async FTask DownloadAssetBundle(AssetBundleVersion assetBundleVersion)
        {
            var downloadByte = await _download.DownloadByte($"{_remotePath}/{assetBundleVersion.Name}");

            if (downloadByte == null || downloadByte.Length == 0)
            {
                Log.Error($"DownloadAssetBundle fail AssetBundle:{assetBundleVersion.Name}");
                return;
            }
            
            // 保存下载的包
            
            var savePath = $"{Define.RemoteAssetBundlePath}/{assetBundleVersion.Name}";
            var saveDirectory = assetBundleVersion.Name.Split('/');
            var currentDirectory = Define.RemoteAssetBundlePath;

            if (saveDirectory.Length > 1)
            {
                for (var i = 0; i < saveDirectory.Length - 1; i++)
                {
                    currentDirectory += $"/{saveDirectory[i]}";

                    if (!Directory.Exists(currentDirectory))
                    {
                        Directory.CreateDirectory(currentDirectory);
                    }
                }
            }
            
            File.WriteAllBytes(savePath, downloadByte);
            _localVersionInfo[assetBundleVersion.Name] = assetBundleVersion;
            _updateFiles.Remove(assetBundleVersion);
        }
    }
}