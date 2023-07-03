using ProtoBuf;

namespace Fantasy
{
    [ProtoContract]
    public abstract class AProto
    {
        public virtual void AfterDeserialization() => EndInit();
        protected virtual void EndInit() { }
    }
}