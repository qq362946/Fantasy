namespace Fantasy
{
    /// <summary>
    /// 当Scene创建完成后发送的事件参数
    /// </summary>
    public struct OnCreateScene
    {
        /// <summary>
        /// 获取与事件关联的场景实体。
        /// </summary>
        public readonly Scene Scene;
        /// <summary>
        /// 初始化一个新的 OnCreateScene 实例。
        /// </summary>
        /// <param name="scene"></param>
        public OnCreateScene(Scene scene)
        {
            Scene = scene;
        }
    }

}