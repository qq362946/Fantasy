namespace Fantasy.IdFactory
{
    /// <summary>
    /// Id扩展工具接口
    /// </summary>
    public interface IIdFactoryTool
    {
        /// <summary>
        /// 获得创建时间
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public uint GetTime(ref long entityId);
        /// <summary>
        /// 获得SceneId
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public uint GetSceneId(ref long entityId);
        /// <summary>
        /// 获得WorldId
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public byte GetWorldId(ref long entityId);
    }
}