namespace Fantasy
{
    /// <summary>
    /// 场景配置信息的类。
    /// </summary>
    public class SceneConfigInfo
    {
        /// <summary>
        /// 获取或设置场景的唯一标识。
        /// </summary>
        public uint Id;

        /// <summary>
        /// 获取或设置场景实体的唯一标识。
        /// </summary>
        public long EntityId;

        /// <summary>
        /// 获取或设置场景类型。
        /// </summary>
        public int SceneType;

        /// <summary>
        /// 获取或设置场景子类型。
        /// </summary>
        public int SceneSubType;

        /// <summary>
        /// 获取或设置场景类型的字符串表示。
        /// </summary>
        public string SceneTypeStr;

        /// <summary>
        /// 获取或设置场景子类型的字符串表示。
        /// </summary>
        public string SceneSubTypeStr;

        /// <summary>
        /// 获取或设置服务器配置的唯一标识。
        /// </summary>
        public uint ServerConfigId;

        /// <summary>
        /// 获取或设置世界的唯一标识。
        /// </summary>
        public uint WorldId;

        /// <summary>
        /// 获取或设置外部端口。
        /// </summary>
        public int OuterPort;

        /// <summary>
        /// 获取或设置网络协议。
        /// </summary>
        public string NetworkProtocol;
    }
}



