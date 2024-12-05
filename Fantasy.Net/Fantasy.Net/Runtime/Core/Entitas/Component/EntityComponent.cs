// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy.Entitas
{
    internal sealed class UpdateQueueInfo
    {
        public bool IsStop;
        public readonly Type Type;
        public readonly long RunTimeId;

        public UpdateQueueInfo(Type type, long runTimeId)
        {
            Type = type;
            IsStop = false;
            RunTimeId = runTimeId;
        }
    }
    
    internal sealed class FrameUpdateQueueInfo
    {
        public readonly Type Type;
        public readonly long RunTimeId;

        public FrameUpdateQueueInfo(Type type, long runTimeId)
        {
            Type = type;
            RunTimeId = runTimeId;
        }
    }
    
    /// <summary>
    /// Entity管理组件
    /// </summary>
    public sealed class EntityComponent : Entity, ISceneUpdate, IAssembly
    {
        private readonly OneToManyList<long, Type> _assemblyList = new();
        private readonly OneToManyList<long, Type> _assemblyHashCodes = new();
        
        private readonly Dictionary<Type, IAwakeSystem> _awakeSystems = new();
        private readonly Dictionary<Type, IUpdateSystem> _updateSystems = new();
        private readonly Dictionary<Type, IDestroySystem> _destroySystems = new();
        private readonly Dictionary<Type, IDeserializeSystem> _deserializeSystems = new();
        private readonly Dictionary<Type, IFrameUpdateSystem> _frameUpdateSystem = new();
        
        private readonly Dictionary<Type, long> _hashCodes = new Dictionary<Type, long>();
        private readonly Queue<UpdateQueueInfo> _updateQueue = new Queue<UpdateQueueInfo>();
        private readonly Queue<FrameUpdateQueueInfo> _frameUpdateQueue = new Queue<FrameUpdateQueueInfo>();
        private readonly Dictionary<long, UpdateQueueInfo> _updateQueueDic = new Dictionary<long, UpdateQueueInfo>();

        internal async FTask<EntityComponent> Initialize()
        {
            await AssemblySystem.Register(this);
            return this;
        }

        #region Assembly

        public FTask Load(long assemblyIdentity)
        {
            var task = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                LoadInner(assemblyIdentity);
                task.SetResult();
            });
            return task;
        }

        public FTask ReLoad(long assemblyIdentity)
        {
            var task = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                LoadInner(assemblyIdentity);
                task.SetResult();
            });
            
            return task;
        }

        public FTask OnUnLoad(long assemblyIdentity)
        {
            var task = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                task.SetResult();
            });
            return task;
        }

        private void LoadInner(long assemblyIdentity)
        {
            foreach (var entityType in AssemblySystem.ForEach(assemblyIdentity, typeof(IEntity)))
            {
                _hashCodes.Add(entityType, HashCodeHelper.ComputeHash64(entityType.FullName));
                _assemblyHashCodes.Add(assemblyIdentity, entityType);
            }
            
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
            if (_assemblyHashCodes.TryGetValue(assemblyIdentity, out var entityType))
            {
                foreach (var type in entityType)
                {
                    _hashCodes.Remove(type);
                }

                _assemblyHashCodes.RemoveByKey(assemblyIdentity);
            }

            if (_assemblyList.TryGetValue(assemblyIdentity, out var assembly))
            {
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
        }

        #endregion

        #region Event

        /// <summary>
        /// 触发实体的唤醒方法
        /// </summary>
        /// <param name="entity">实体对象</param>
        public void Awake(Entity entity)
        {
            if (!_awakeSystems.TryGetValue(entity.Type, out var awakeSystem))
            {
                return;
            }

            try
            {
                awakeSystem.Invoke(entity);
            }
            catch (Exception e)
            {
                Log.Error($"{entity.Type.FullName} Error {e}");
            }
        }
        
        /// <summary>
        /// 触发实体的销毁方法
        /// </summary>
        /// <param name="entity">实体对象</param>
        public void Destroy(Entity entity)
        {
            if (!_destroySystems.TryGetValue(entity.Type, out var system))
            {
                return;
            }

            try
            {
                system.Invoke(entity);
            }
            catch (Exception e)
            {
                Log.Error($"{entity.Type.FullName} Destroy Error {e}");
            }
        }
        
        /// <summary>
        /// 触发实体的反序列化方法
        /// </summary>
        /// <param name="entity">实体对象</param>
        public void Deserialize(Entity entity) 
        {
            if (!_deserializeSystems.TryGetValue(entity.Type, out var system))
            {
                return;
            }

            try
            {
                system.Invoke(entity);
            }
            catch (Exception e)
            {
                Log.Error($"{entity.Type.FullName} Deserialize Error {e}");
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
            var type = entity.Type;
            var entityRuntimeId = entity.RuntimeId;

            if (_updateSystems.ContainsKey(type))
            {
                var updateQueueInfo = new UpdateQueueInfo(type, entityRuntimeId);
                _updateQueue.Enqueue(updateQueueInfo);
                _updateQueueDic.Add(entityRuntimeId, updateQueueInfo);
            }

            if (_frameUpdateSystem.ContainsKey(type))
            {
                _frameUpdateQueue.Enqueue(new FrameUpdateQueueInfo(type, entityRuntimeId));
            }
        }

        /// <summary>
        /// 停止实体进行更新
        /// </summary>
        /// <param name="entity">实体对象</param>
        public void StopUpdate(Entity entity)
        {
            if (!_updateQueueDic.Remove(entity.RuntimeId, out var updateQueueInfo))
            {
                return;
            }

            updateQueueInfo.IsStop = true;
        }

        /// <summary>
        /// 执行实体系统的更新逻辑
        /// </summary>
        public void Update()
        {
            var updateQueueCount = _updateQueue.Count;

            while (updateQueueCount-- > 0)
            {
                var updateQueueStruct = _updateQueue.Dequeue();

                if (updateQueueStruct.IsStop)
                {
                    continue;
                }
                
                if (!_updateSystems.TryGetValue(updateQueueStruct.Type, out var updateSystem))
                {
                    continue;
                }
                
                var entity = Scene.GetEntity(updateQueueStruct.RunTimeId);
                
                if (entity == null || entity.IsDisposed)
                {
                    _updateQueueDic.Remove(updateQueueStruct.RunTimeId);
                    continue;
                }
                
                _updateQueue.Enqueue(updateQueueStruct);
                
                try
                {
                    updateSystem.Invoke(entity);
                }
                catch (Exception e)
                {
                    Log.Error($"{updateQueueStruct.Type.FullName} Update Error {e}");
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
                var frameUpdateQueueStruct = _frameUpdateQueue.Dequeue();
                
                if (!_frameUpdateSystem.TryGetValue(frameUpdateQueueStruct.Type, out var frameUpdateSystem))
                {
                    continue;
                }
                
                var entity = Scene.GetEntity(frameUpdateQueueStruct.RunTimeId);

                if (entity == null || entity.IsDisposed)
                {
                    continue;
                }

                _frameUpdateQueue.Enqueue(frameUpdateQueueStruct);

                try
                {
                    frameUpdateSystem.Invoke(entity);
                }
                catch (Exception e)
                {
                    Log.Error($"{frameUpdateQueueStruct.Type.FullName} FrameUpdate Error {e}");
                }
            }
        }

        #endregion

        public long GetHashCode(Type type)
        {
            return _hashCodes[type];
        }

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