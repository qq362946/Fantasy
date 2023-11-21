using System;

namespace Fantasy
{
    /// <summary>
    /// 继承这个的Message协议会在序列化后回收到对象池中
    /// 创建消息请使用Pool.Rent来创建消息、否则会造成内存泄露
    /// </summary>
    public interface IPoolMessage : IDisposable
    {
    
    }
}