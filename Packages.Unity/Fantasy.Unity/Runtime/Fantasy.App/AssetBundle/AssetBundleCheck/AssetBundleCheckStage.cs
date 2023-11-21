namespace Fantasy
{
    public enum AssetBundleCheckStage
    {
        /// <summary>
        /// 下载资源版本文件MD5
        /// </summary>
        CheckVersionMD5 = 1,
        /// <summary>
        /// 下载资源版本文件来对比需要更新的资源列表
        /// </summary>
        DownloadVersion = 2,
        /// <summary>
        /// 下载资源
        /// </summary>
        DownloadAssetBundle = 3,
        /// <summary>
        /// 更新完成
        /// </summary>
        Complete = 4,
        /// <summary>
        /// 更新失败
        /// </summary>
        UpdateFailed = 5,
    }
}