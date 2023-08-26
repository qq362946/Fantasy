namespace Fantasy.Helper
{
    /// <summary>
    /// 定义一个可更新的单例接口，继承自 <see cref="ISingleton"/>。
    /// </summary>
    public interface IUpdateSingleton : ISingleton
    {
        /// <summary>
        /// 更新单例实例的方法。
        /// </summary>
        public abstract void Update();
    }
}