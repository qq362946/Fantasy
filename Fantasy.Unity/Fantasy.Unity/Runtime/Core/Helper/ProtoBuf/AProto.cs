using System.ComponentModel;
using ProtoBuf;

namespace Fantasy
{
    /// <summary>
    /// 提供 ProtoBuff 序列化和反序列化支持的接口。
    /// </summary>
    public interface IProto
    {
        
    }
    /// <summary>
    /// 提供 ProtoBuff 序列化和反序列化支持的抽象基类。
    /// </summary>
    [ProtoContract]
    public abstract class AProto : IProto
    {
        /// <summary>
        /// 在反序列化完成后执行的操作，可以在子类中重写以完成初始化。
        /// </summary>
        public virtual void AfterDeserialization() => EndInit();
        /// <summary>
        /// 在 <see cref="AfterDeserialization"/> 中调用，用于完成子类的初始化操作。
        /// </summary>
        protected virtual void EndInit() { }
    }
}