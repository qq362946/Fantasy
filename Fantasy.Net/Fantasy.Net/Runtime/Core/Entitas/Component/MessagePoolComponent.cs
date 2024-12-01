// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas;
using Fantasy.Pool;
using Fantasy.Serialize;

namespace Fantasy.Entitas
{
    /// <summary>
    /// 消息的对象池组件
    /// </summary>
    public sealed class MessagePoolComponent : Entity
    {
        private int _poolCount;
        private const int MaxCapacity = ushort.MaxValue;
        private readonly OneToManyQueue<Type, AMessage> _poolQueue = new OneToManyQueue<Type, AMessage>();
        private readonly Dictionary<Type, Func<AMessage>> _typeCheckCache = new Dictionary<Type, Func<AMessage>>();
        /// <summary>
        /// 销毁组件
        /// </summary>
        public override void Dispose()
        {
            _poolCount = 0;
            _poolQueue.Clear();
            _typeCheckCache.Clear();
            base.Dispose();
        }
        /// <summary>
        /// 从对象池里获取一个消息，如果没有就创建一个新的
        /// </summary>
        /// <typeparam name="T">消息的泛型类型</typeparam>
        /// <returns></returns>
        public T Rent<T>() where T : AMessage, new()
        {
            if (!_poolQueue.TryDequeue(typeof(T), out var queue))
            {
                var instance = new T();
                instance.SetScene(Scene);
                instance.SetIsPool(true);
                return instance;
            }

            queue.SetIsPool(true);
            _poolCount--;
            return (T)queue;
        }

        /// <summary>
        /// <see cref="Rent"/>
        /// </summary>
        /// <param name="type">消息的类型</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AMessage Rent(Type type)
        {
            if (!_poolQueue.TryDequeue(type, out var queue))
            {
                if (!_typeCheckCache.TryGetValue(type, out var createInstance))
                {
                    if (!typeof(AMessage).IsAssignableFrom(type))
                    {
                        throw new NotSupportedException($"{this.GetType().FullName} Type:{type.FullName} must inherit from IPool");
                    }
                    else
                    {
                        createInstance = CreateInstance.CreateMessage(type);
                        _typeCheckCache[type] = createInstance;
                    }
                }
                
                var instance = createInstance();
                instance.SetScene(Scene);
                instance.SetIsPool(true);
                return instance;
            }
            
            queue.SetIsPool(true);
            _poolCount--;
            return queue;
        }
        /// <summary>
        /// 返还一个消息到对象池中
        /// </summary>
        /// <param name="obj"></param>
        public void Return(AMessage obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!obj.IsPool())
            {
                return;
            }

            if (_poolCount >= MaxCapacity)
            {
                return;
            }
            
            _poolCount++;
            obj.SetIsPool(false);
            _poolQueue.Enqueue(obj.GetType(), obj);
        }

        /// <summary>
        /// <see cref="Return"/>
        /// </summary>
        /// <param name="obj">返还的消息</param>
        /// <typeparam name="T">返还的消息泛型类型</typeparam>
        public void Return<T>(T obj) where T : AMessage
        {
            if (obj == null)
            {
                return;
            }

            if (!obj.IsPool())
            {
                return;
            }

            if (_poolCount >= MaxCapacity)
            {
                return;
            }
            
            _poolCount++;
            obj.SetIsPool(false);
            _poolQueue.Enqueue(typeof(T), obj);
        }
    }
}