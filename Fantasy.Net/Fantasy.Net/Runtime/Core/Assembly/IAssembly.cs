using System;

namespace Fantasy
{
    /// <summary>
    /// 实现这个接口、会再程序集首次加载、卸载、重载的时候调用
    /// </summary>
    public interface IAssembly : IDisposable
    {
        /// <summary>
        /// 程序集加载时调用
        /// </summary>
        /// <param name="assemblyIdentity">程序集标识</param>
        public FTask Load(long assemblyIdentity);
        /// <summary>
        /// 程序集重新加载的时候调用
        /// </summary>
        /// <param name="assemblyIdentity">程序集标识</param>
        public FTask ReLoad(long assemblyIdentity);
        /// <summary>
        /// 卸载的时候调用
        /// </summary>
        /// <param name="assemblyIdentity">程序集标识</param>
        public FTask OnUnLoad(long assemblyIdentity);
    }
}