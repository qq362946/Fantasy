// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
    /// <summary>
    /// Entity管理组件
    /// </summary>
    public sealed class EntityComponent : Entity, IAssembly
    {
        private readonly OneToManyList<long, Type> _assemblyList = new();
        private readonly Dictionary<Type, IAwakeSystem> _awakeSystems = new();
        private readonly Dictionary<Type, IUpdateSystem> _updateSystems = new();
        private readonly Dictionary<Type, IDestroySystem> _destroySystems = new();
        private readonly Dictionary<Type, IEntitiesSystem> _deserializeSystems = new();
        private readonly Dictionary<Type, IFrameUpdateSystem> _frameUpdateSystem = new();

        private readonly Queue<long> _updateQueue = new Queue<long>();
        private readonly Queue<long> _frameUpdateQueue = new Queue<long>();

        public async FTask Initialize()
        {
            await AssemblySystem.Register(this);
        }

        #region Assembly

        public Task Load(long assemblyIdentity)
        {
            LoadInner(assemblyIdentity);
            return Task.CompletedTask;
        }

        public Task ReLoad(long assemblyIdentity)
        {
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                LoadInner(assemblyIdentity);
            });
            
            return Task.CompletedTask;
        }

        public Task OnUnLoad(long assemblyIdentity)
        {
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
            });
            
            return Task.CompletedTask;
        }

        private void LoadInner(long assemblyIdentity)
        {
            foreach (var entitiesSystemType in AssemblySystem.ForEach(assemblyIdentity, typeof(IEntitiesSystem)))
            {
                Type entitiesType = null;
                var entity = Activator.CreateInstance(entitiesSystemType);

                switch (entity)
                {
                    case IAwakeSystem iAwakeSystem:
                    {
                        entitiesType = iAwakeSystem.EntitiesType();
                        _awakeSystems.Add(entitiesType, iAwakeSystem);
                        break;
                    }
                    case IDestroySystem iDestroySystem:
                    {
                        entitiesType = iDestroySystem.EntitiesType();
                        _destroySystems.Add(entitiesType, iDestroySystem);
                        break;
                    }
                    case IDeserializeSystem iDeserializeSystem:
                    {
                        entitiesType = iDeserializeSystem.EntitiesType();
                        _deserializeSystems.Add(entitiesType, iDeserializeSystem);
                        break;
                    }
                    case IUpdateSystem iUpdateSystem:
                    {
                        entitiesType = iUpdateSystem.EntitiesType();
                        _updateSystems.Add(entitiesType, iUpdateSystem);
                        break;
                    }
                    case IFrameUpdateSystem iFrameUpdateSystem:
                    {
                        entitiesType = iFrameUpdateSystem.EntitiesType();
                        _frameUpdateSystem.Add(entitiesType, iFrameUpdateSystem);
                        break;
                    }
                    default:
                    {
                        Log.Error($"IEntitiesSystem not support type {entitiesSystemType}");
                        return;
                    }
                }

                _assemblyList.Add(assemblyIdentity, entitiesType);
            }
        }

        private void OnUnLoadInner(long assemblyIdentity)
        {
            if (!_assemblyList.TryGetValue(assemblyIdentity, out var assembly))
            {
                return;
            }
            
            foreach (var type in assembly)
            {
                _awakeSystems.Remove(type);
                _updateSystems.Remove(type);
                _destroySystems.Remove(type);
                _deserializeSystems.Remove(type);
                _frameUpdateSystem.Remove(type);
            }
            
            _assemblyList.RemoveByKey(assemblyIdentity);
        }

        #endregion

        #region Event

        /// <summary>
        /// 触发实体的唤醒方法
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        public void Awake<T>(T entity) where T : Entity
        {
            var type = entity.GetType();

            if (!_awakeSystems.TryGetValue(type, out var awakeSystem))
            {
                return;
            }

            try
            {
                awakeSystem.Invoke(entity);
            }
            catch (Exception e)
            {
                Log.Error($"{type.Name} Error {e}");
            }
        }

        /// <summary>
        /// 触发实体的唤醒方法
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="T1">参数类型</typeparam>
        /// <param name="entity">实体对象</param>
        /// <param name="ages">参数</param>
        public void Awake<T, T1>(T entity, T1 ages) where T : Entity where T1 : struct
        {
            var type = entity.GetType();

            if (!_awakeSystems.TryGetValue(type, out var awakeSystem))
            {
                return;
            }

            try
            {
                if (awakeSystem is not AwakeSystem<T, T1> system)
                {
                    return;
                }

                system.Invoke(entity, ages);
            }
            catch (Exception e)
            {
                Log.Error($"{type.Name} Awake Error {e}");
            }
        }

        /// <summary>
        /// 触发实体的销毁方法
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        public void Destroy<T>(T entity) where T : Entity
        {
            var type = entity.GetType();

            if (!_destroySystems.TryGetValue(type, out var system))
            {
                return;
            }

            try
            {
                system.Invoke(entity);
            }
            catch (Exception e)
            {
                Log.Error($"{type.Name} Destroy Error {e}");
            }
        }

        /// <summary>
        /// 触发实体的反序列化方法
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        public void Deserialize<T>(T entity) where T : Entity
        {
            var type = entity.GetType();

            if (!_deserializeSystems.TryGetValue(type, out var system))
            {
                return;
            }

            try
            {
                system.Invoke(entity);
            }
            catch (Exception e)
            {
                Log.Error($"{type.Name} Deserialize Error {e}");
            }
        }

        #endregion

        #region Update

        /// <summary>
        /// 将实体加入更新队列，准备进行更新
        /// </summary>
        /// <param name="entity">实体对象</param>
        public void StartUpdate(Entity entity)
        {
            var type = entity.GetType();
            var entityRuntimeId = entity.RuntimeId;

            if (_updateSystems.ContainsKey(type))
            {
                _updateQueue.Enqueue(entityRuntimeId);
            }

            if (_frameUpdateSystem.ContainsKey(type))
            {
                _frameUpdateQueue.Enqueue(entityRuntimeId);
            }
        }

        /// <summary>
        /// 执行实体系统的更新逻辑
        /// </summary>
        public void Update()
        {
            var updateQueueCount = _updateQueue.Count;

            while (updateQueueCount-- > 0)
            {
                var runtimeId = _updateQueue.Dequeue();
                var entity = Scene.GetEntity(runtimeId);

                if (entity == null || entity.IsDisposed)
                {
                    continue;
                }

                var type = entity.GetType();

                if (!_updateSystems.TryGetValue(type, out var updateSystem))
                {
                    continue;
                }

                _updateQueue.Enqueue(runtimeId);

                try
                {
                    updateSystem.Invoke(entity);
                }
                catch (Exception e)
                {
                    Log.Error($"{type} Update Error {e}");
                }
            }
        }

        /// <summary>
        /// 执行实体系统的帧更新逻辑
        /// </summary>
        public void FrameUpdate()
        {
            var count = _frameUpdateQueue.Count;

            while (count-- > 0)
            {
                var runtimeId = _frameUpdateQueue.Dequeue();
                var entity = Scene.GetEntity(runtimeId);

                if (entity == null || entity.IsDisposed)
                {
                    continue;
                }

                var type = entity.GetType();

                if (!_frameUpdateSystem.TryGetValue(type, out var frameUpdateSystem))
                {
                    continue;
                }

                _frameUpdateQueue.Enqueue(runtimeId);

                try
                {
                    frameUpdateSystem.Invoke(entity);
                }
                catch (Exception e)
                {
                    Log.Error($"{type} FrameUpdate Error {e}");
                }
            }
        }

        #endregion

        /// <summary>
        /// 释放实体系统管理器资源
        /// </summary>
        public override void Dispose()
        {
            _updateQueue.Clear();
            _frameUpdateQueue.Clear();

            _assemblyList.Clear();
            _awakeSystems.Clear();
            _updateSystems.Clear();
            _destroySystems.Clear();
            _deserializeSystems.Clear();
            _frameUpdateSystem.Clear();

            AssemblySystem.UnRegister(this);
            base.Dispose();
        }
    }
}