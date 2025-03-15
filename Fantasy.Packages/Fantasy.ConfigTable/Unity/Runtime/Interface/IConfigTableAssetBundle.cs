namespace Fantasy.ConfigTable
{
    public interface IConfigTableAssetBundle
    {
        /// <summary>
        /// 不同的资源管理器有各自独特的包加载方式。
        /// 为了满足不同的需求，我们提供了这个接口，允许用户自定义拼装包路径。
        /// 通过这个接口，用户可以根据自己的特定格式和要求，灵活地加载包，从而提高资源管理的灵活性和效率。
        /// </summary>
        /// <param name="assetBundleDirectoryPath"></param>
        /// <param name="dataConfig"></param>
        /// <returns></returns>
        public string Combine(string assetBundleDirectoryPath, string dataConfig);
        public byte[] LoadConfigTable(string assetBundlePath);
    }
}