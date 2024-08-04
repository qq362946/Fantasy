using System;

namespace Fantasy
{
    /// <summary>
    /// 支持再一个组件里添加多个同类型组件
    /// </summary>
    public interface ISupportedMultiEntity : IDisposable { }
    
    public static class SupportedMultiEntityChecker<T> where T : Entity
    {
        public static bool IsSupported { get; }

        static SupportedMultiEntityChecker()
        {
            IsSupported = typeof(ISupportedMultiEntity).IsAssignableFrom(typeof(T));
        }
    }
}