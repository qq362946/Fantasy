using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Entitas.Interface
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