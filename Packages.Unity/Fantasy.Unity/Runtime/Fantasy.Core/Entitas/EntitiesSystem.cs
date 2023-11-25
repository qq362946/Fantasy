using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fantasy
{
    /// <summary>
    /// 实体系统管理器，用于管理各种实体系统的生命周期和更新
    /// </summary>
    public sealed class EntitiesSystem : Singleton<EntitiesSystem>, IUpdateSingleton
    {
        private readonly OneToManyList<int, Type> _assemblyList = new();
        private readonly Dictionary<Type, IAwakeSystem> _awakeSystems = new();
        private readonly Dictionary<Type, IUpdateSystem> _updateSystems = new();
        private readonly Dictionary<Type, IDestroySystem> _destroySystems = new();
        private readonly Dictionary<Type, IEntitiesSystem> _deserializeSystems = new();
        private readonly Dictionary<Type, IFrameUpdateSystem> _frameUpdateSystem = new();

        private readonly Queue<long> _updateQueue = new Queue<long>();
        private readonly Queue<long> _frameUpdateQueue = new Queue<long>();

        /// <summary>
        /// 当加载程序集时的处理方法，用于初始化实体系统列表
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        protected override void OnLoad(int assemblyName)
        {
            foreach (var entitiesSystemType in AssemblyManager.ForEach(assemblyName, typeof(IEntitiesSystem)))
            {
                var entity = Activator.CreateInstance(entitiesSystemType);

                switch (entity)
                {
                    case IAwakeSystem iAwakeSystem:
                    {
                        _awakeSystems.Add(iAwakeSystem.EntitiesType(), iAwakeSystem);
                        break;
                    }
                    case IDestroySystem iDestroySystem:
                    {
                        _destroySystems.Add(iDestroySystem.EntitiesType(), iDestroySystem);
                        break;
                    }
                    case IDeserializeSystem iDeserializeSystem:
                    {
                        _deserializeSystems.Add(iDeserializeSystem.EntitiesType(), iDeserializeSystem);
                        break;
                    }
                    case IUpdateSystem iUpdateSystem:
                    {
                        _updateSystems.Add(iUpdateSystem.EntitiesType(), iUpdateSystem);
                        break;
                    }
                    case IFrameUpdateSystem iFrameUpdateSystem:
                    {
                        _frameUpdateSystem.Add(iFrameUpdateSystem.EntitiesType(), iFrameUpdateSystem);
                        break;
                    }
                }

                _assemblyList.Add(assemblyName, entitiesSystemType);
            }
        }

        /// <summary>
        /// 当卸载程序集时的处理方法，用于清理实体系统列表
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        protected override void OnUnLoad(int assemblyName)
        {
            if (!_assemblyList.TryGetValue(assemblyName, out var assembly))
            {
                return;
            }

            _assemblyList.RemoveByKey(assemblyName);
            
            foreach (var type in assembly)
            {
                _awakeSystems.Remove(type);
                _updateSystems.Remove(type);
                _destroySystems.Remove(type);
                _deserializeSystems.Remove(type);
                _frameUpdateSystem.Remove(type);
            }
        }

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
                Log.Error($"{type.Name} Error {e}");
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
                Log.Error($"{type.Name} Error {e}");
            }
        }

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
        /// 执行实体系统的帧更新逻辑
        /// </summary>
        public void FrameUpdate(int frameDeltaTime)
        {
            var count = _frameUpdateQueue.Count;

            while (count-- > 0)
            {
                var runtimeId = _frameUpdateQueue.Dequeue();
                var entity = Entity.GetEntity(runtimeId);
                
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
                    Log.Error($"{type} Error {e}");
                }
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
                var entity = Entity.GetEntity(runtimeId);

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
                    Log.Error($"{type} Error {e}");
                }
            }
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
            
            AssemblyManager.OnLoadAssemblyEvent -= OnLoad;
            AssemblyManager.OnUnLoadAssemblyEvent -= OnUnLoad;
            base.Dispose();
        }
    }
}