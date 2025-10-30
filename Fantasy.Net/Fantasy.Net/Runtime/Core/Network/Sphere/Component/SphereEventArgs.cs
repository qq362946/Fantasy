#if FANTASY_NET
using System.Runtime.Serialization;
using Fantasy.Entitas.Interface;
using Fantasy.Pool;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using ProtoBuf;

// ReSharper disable CheckNamespace
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Fantasy.Sphere;

/// <summary>
/// Sphere事件参数对象池
/// Object pool for Sphere event arguments
/// </summary>
internal sealed class SphereEventArgPool() : PoolCore(4096);

/// <summary>
/// Sphere事件参数基类，用于跨进程事件传递
/// Base class for Sphere event arguments, used for cross-process event delivery
/// </summary>
public abstract class SphereEventArgs : IPool, IDisposable
{
    /// <summary>
    /// 静态对象池实例
    /// Static object pool instance
    /// </summary>
    private static readonly SphereEventArgPool ArgPool = new SphereEventArgPool();

    /// <summary>
    /// 标识对象是否来自对象池
    /// Indicates whether the object is from the object pool
    /// </summary>
    private bool _isPool;

    /// <summary>
    /// 类型哈希码，用于快速类型识别
    /// Type hash code for fast type identification
    /// </summary>
    public long TypeHashCode { get; private set; }

    /// <summary>
    /// 事件参数的实际类型
    /// The actual type of the event arguments
    /// </summary>
    [BsonIgnore]
    [JsonIgnore]
    [IgnoreDataMember]
    [ProtoIgnore]
    public Type SphereEventArgsType { get; private set; }

    /// <summary>
    /// Protected 构造函数,防止外部直接实例化
    /// Protected constructor to prevent direct instantiation from outside
    /// </summary>
    protected SphereEventArgs() { }

    /// <summary>
    /// 创建事件参数实例
    /// Create an instance of event arguments
    /// </summary>
    /// <typeparam name="T">事件参数类型 / Event argument type</typeparam>
    /// <param name="isFromPool">是否从对象池获取 / Whether to get from object pool</param>
    /// <returns>事件参数实例 / Event argument instance</returns>
    public static T Create<T>(bool isFromPool) where T : SphereEventArgs, new()
    {
        var sphereEventArgs = isFromPool ? ArgPool.Rent<T>() : new T();
        sphereEventArgs.SphereEventArgsType = typeof(T);
        sphereEventArgs.TypeHashCode = TypeHashCache<T>.HashCode;
        return sphereEventArgs;
    }

    /// <summary>
    /// 释放事件参数，将其返回对象池
    /// Dispose the event arguments and return it to the object pool
    /// </summary>
    public void Dispose()
    {
        ArgPool.Return(SphereEventArgsType, this);
        TypeHashCode = 0;
        SphereEventArgsType = null;
    }

    /// <summary>
    /// 判断对象是否来自对象池
    /// Check if the object is from the object pool
    /// </summary>
    /// <returns>true表示来自对象池 / true if from object pool</returns>
    public bool IsPool()
    {
        return _isPool;
    }

    /// <summary>
    /// 设置对象池标识
    /// Set the object pool flag
    /// </summary>
    /// <param name="isPool">是否来自对象池 / Whether from object pool</param>
    public void SetIsPool(bool isPool)
    {
        _isPool = isPool;
    }
}
#endif