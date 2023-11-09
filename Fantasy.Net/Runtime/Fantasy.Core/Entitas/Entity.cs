using System;
using System.Collections.Generic;
using Fantasy.Helper;
using System.Runtime.Serialization;
using Fantasy.DataStructure;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable CS8601
#pragma warning disable CS8603

// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable InconsistentNaming

namespace Fantasy
{
    /// <summary>
    /// 实体基类，用于实体创建、回收、获取，和组件操作
    /// </summary>
    public abstract class Entity : IDisposable
    {
        #region Entities
        
        private static readonly Dictionary<long, Entity> Entities = new Dictionary<long, Entity>();
        private static readonly OneToManyQueue<Type, Entity> Pool = new OneToManyQueue<Type, Entity>();

        /// <summary>
        /// 获取指定运行时ID的实体对象
        /// </summary>
        /// <param name="runTimeId">运行时ID</param>
        /// <returns>实体对象</returns>
        public static Entity GetEntity(long runTimeId)
        {
            return Entities.TryGetValue(runTimeId, out var entity) ? entity : null;
        }

        /// <summary>
        /// 尝试获取指定运行时ID的实体对象
        /// </summary>
        /// <param name="runTimeId">运行时ID</param>
        /// <param name="entity">输出参数，实体对象</param>
        /// <returns>是否获取成功</returns>
        public static bool TryGetEntity(long runTimeId, out Entity entity)
        {
            return Entities.TryGetValue(runTimeId, out entity);
        }

        /// <summary>
        /// 获取指定运行时ID的实体对象。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="runTimeId">要获取的实体的运行时ID。</param>
        /// <returns>找到的实体对象，如果不存在则返回默认值。</returns>
        public static T GetEntity<T>(long runTimeId) where T : Entity, new()
        {
            if (!Entities.TryGetValue(runTimeId, out var entity))
            {
                return default;
            }

            return (T) entity;
        }

        /// <summary>
        /// 尝试获取指定运行时ID的实体对象。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="runTimeId">要获取的实体的运行时ID。</param>
        /// <param name="outEntity">输出参数，找到的实体对象。</param>
        /// <returns>如果找到实体对象则返回 true，否则返回 false。</returns>
        public static bool TryGetEntity<T>(long runTimeId, out T outEntity) where T : Entity, new()
        {
            if (!Entities.TryGetValue(runTimeId, out var entity))
            {
                outEntity = default;
                return false;
            }

            outEntity = (T) entity;
            return true;
        }

        private static T Rent<T>(Type entityType) where T : Entity, new()
        {
            if (typeof(INotSupportedPool).IsAssignableFrom(entityType))
            {
                return Activator.CreateInstance<T>();
            }

            T entity;

            if (Pool.TryDequeue(entityType, out var poolEntity))
            {
                entity = (T) poolEntity;
            }
            else
            {
                entity = Activator.CreateInstance<T>();
            }

            entity._isFromPool = true;
            return entity;
        }

        private static void Return(Entity entity)
        {
            entity.Id = 0;
            
            if (!entity._isFromPool)
            {
                return;
            }

            entity._isFromPool = false;
            Pool.Enqueue(entity.GetType(), entity);
        }

        #endregion

        #region Create
        /// <summary>
        /// 在指定场景中创建一个实体对象，并触发相关事件（可选）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="scene">要创建实体的场景。</param>
        /// <param name="isRunEvent">是否触发相关事件。</param>
        /// <returns>创建的实体对象。</returns>
        public static T Create<T>(Scene scene, bool isRunEvent = true) where T : Entity, new()
        {
            var entity = Create<T>(scene.LocationId, isRunEvent);
            entity.Scene = scene;
            return entity;
        }

        /// <summary>
        /// 在指定场景中创建一个具有指定ID的实体对象，并触发相关事件（可选）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="scene">要创建实体的场景。</param>
        /// <param name="id">要分配给实体的ID。</param>
        /// <param name="isRunEvent">是否触发相关事件。</param>
        /// <returns>创建的实体对象。</returns>
        public static T Create<T>(Scene scene, long id, bool isRunEvent = true) where T : Entity, new()
        {
            var entity = Create<T>(id, scene.LocationId, isRunEvent);
            entity.Scene = scene;
            return entity;
        }

        private static T Create<T>(uint locationId, bool isRunEvent = true) where T : Entity, new()
        {
            var entity = Rent<T>(typeof(T));
#if FANTASY_NET
            entity.Id = entity.RuntimeId = IdFactory.NextEntityId(locationId);
#else
            entity.Id = entity.RuntimeId = IdFactory.NextRunTimeId();
#endif
            Entities.Add(entity.RuntimeId, entity);

            if (isRunEvent)
            {
                EntitiesSystem.Instance.Awake(entity);
                EntitiesSystem.Instance.StartUpdate(entity);
            }

            return entity;
        }

        /// <summary>
        /// 在指定位置（locationId）上创建一个具有指定ID的实体对象，并触发相关事件（可选）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="id">要分配给实体的ID。</param>
        /// <param name="locationId">实体所在位置的ID。</param>
        /// <param name="isRunEvent">是否触发相关事件。</param>
        /// <returns>创建的实体对象。</returns>
        protected static T Create<T>(long id, uint locationId, bool isRunEvent = true) where T : Entity, new()
        {
            return Create<T>(id, IdFactory.NextEntityId(locationId), isRunEvent);
        }

        /// <summary>
        /// 在指定位置中创建一个实体对象，并可选择是否立即触发事件。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="id">要分配给实体的ID。</param>
        /// <param name="runtimeId">要分配给实体的运行时ID。</param>
        /// <param name="isRunEvent">是否立即触发实体事件。</param>
        /// <returns>创建的实体对象。</returns>
        protected static T Create<T>(long id, long runtimeId, bool isRunEvent = true) where T : Entity, new()
        {
            var entity = Rent<T>(typeof(T));
            entity.Id = id;
            entity.RuntimeId = runtimeId;
            Entities.Add(entity.RuntimeId, entity);

            if (isRunEvent)
            {
                EntitiesSystem.Instance.Awake(entity);
                EntitiesSystem.Instance.StartUpdate(entity);
            }

            return entity;
        }

        #endregion

        #region Members
        /// <summary>
        /// 获取或设置实体的唯一ID。
        /// </summary>
        [BsonId]
        [BsonElement]
        [BsonIgnoreIfDefault]
        [BsonDefaultValue(0L)]
        public long Id { get; private set; }

        /// <summary>
        /// 获取实体的运行时ID。
        /// </summary>
        [BsonIgnore] 
        [IgnoreDataMember]
        public long RuntimeId { get; private set; }

        /// <summary>
        /// 获取一个值，表示实体是否已被释放。
        /// </summary>
        [BsonIgnore] 
        [JsonIgnore]
        [IgnoreDataMember] 
        public bool IsDisposed => RuntimeId == 0;

        /// <summary>
        /// 获取或设置实体所属的场景。
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        public Scene Scene { get; protected set; }

        /// <summary>
        /// 获取或设置实体的父实体。
        /// </summary>
        [BsonIgnore] 
        [JsonIgnore]
        [IgnoreDataMember]
        public Entity Parent { get; protected set; }

        [BsonElement("t")] 
        [BsonIgnoreIfNull] 
        private ListPool<Entity> _treeDb;
        
        [BsonIgnore] 
        [IgnoreDataMember] 
        private DictionaryPool<Type, Entity> _tree;
        
        [BsonElement("m")] 
        [BsonIgnoreIfNull] 
        private ListPool<Entity> _multiDb;
        
        [BsonIgnore] 
        [IgnoreDataMember] 
        private DictionaryPool<long, ISupportedMultiEntity> _multi;

        [BsonIgnore] 
        [IgnoreDataMember] 
        private bool _isFromPool;
        
        /// <summary>
        /// 获取当前实体的父实体。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetParent<T>() where T : Entity, new()
        {
            return Parent as T;
        }

        #endregion

        #region AddComponent
        /// <summary>
        /// 在当前实体上添加一个指定类型的组件，并立即触发组件事件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>创建的组件实体。</returns>
        public T AddComponent<T>() where T : Entity, new()
        {
            var entity = Create<T>(Scene.LocationId, false);
            AddComponent(entity);
            EntitiesSystem.Instance.Awake(entity);
            EntitiesSystem.Instance.StartUpdate(entity);
            return entity;
        }

        /// <summary>
        /// 在当前实体上添加一个指定类型的组件，并立即触发组件事件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="id">要分配给组件的ID。</param>
        /// <returns>创建的组件实体。</returns>
        public T AddComponent<T>(long id) where T : Entity, new()
        {
            var entity = Create<T>(id, Scene.LocationId, false);
            AddComponent(entity);
            EntitiesSystem.Instance.Awake(entity);
            EntitiesSystem.Instance.StartUpdate(entity);
            return entity;
        }

        /// <summary>
        /// 将指定的组件添加到当前实体。
        /// </summary>
        /// <param name="component">要添加的组件。</param>
        public void AddComponent(Entity component)
        {
            if (this == component)
            {
                Log.Error("Cannot add oneself to one's own components");
                return;
            }
            
            if (component.IsDisposed)
            {
                Log.Error($"component is Disposed {component.GetType().FullName}");
                return;
            }
            
            var type = component.GetType();
            component.Parent?.RemoveComponent(component, false);
            
            if (component is ISupportedMultiEntity multiEntity)
            {
                _multi ??= DictionaryPool<long, ISupportedMultiEntity>.Create();
                _multi.Add(component.Id, multiEntity);

                if (component is ISupportedDataBase)
                {
                    _multiDb ??= ListPool<Entity>.Create();
                    _multiDb.Add(component);
                }
            }
            else
            {
#if FANTASY_NET
                if (component is ISupportedSingleCollection && component.Id != Id)
                {
                    Log.Error($"component type :{component.GetType().FullName} for implementing ISupportedSingleCollection, it is required that the Id must be the same as the parent");
                }
#endif
                if (_tree == null)
                {
                    _tree = DictionaryPool<Type, Entity>.Create();
                }
                else if(_tree.ContainsKey(type))
                {
                    Log.Error($"type:{type.FullName} If you want to add multiple components of the same type, please implement IMultiEntity");
                    return;
                }
                
                _tree.Add(type, component);

                if (component is ISupportedDataBase)
                {
                    _treeDb ??= ListPool<Entity>.Create();
                    _treeDb.Add(component);
                }
            }

            component.Parent = this;
            component.Scene = Scene;
        }

        #endregion
        
        #region ForEach
#if FANTASY_NET
        /// <summary>
        /// 获取一个 IEnumerable，用于遍历当前实体上所有实现了 ISupportedSingleCollection 接口的组件。
        /// </summary>
        public IEnumerable<Entity> ForEachSingleCollection
        {
            get
            {
                foreach (var (_, treeEntity) in _tree)
                {
                    if (treeEntity is not ISupportedSingleCollection)
                    {
                        continue;
                    }

                    yield return treeEntity;
                }
            }
        }

        /// <summary>
        /// 获取一个 IEnumerable，用于遍历当前实体上所有实现了 ISupportedSingleCollection 或 ISupportedTransfer 接口的组件。
        /// </summary>
        public IEnumerable<Entity> ForEachTransfer
        {
            get
            {
                if (_tree != null)
                {
                    foreach (var (_, treeEntity) in _tree)
                    {
                        if (treeEntity is ISupportedSingleCollection || treeEntity is ISupportedTransfer)
                        {
                            yield return treeEntity;
                        }
                    }
                }

                if (_multiDb != null)
                {
                    foreach (var treeEntity in _multiDb)
                    {
                        if (treeEntity is not ISupportedTransfer)
                        {
                            continue;
                        }

                        yield return treeEntity;
                    }
                }
            }
        }
#endif
        /// <summary>
        /// 获取一个 IEnumerable，用于遍历当前实体上所有实现了ISupportedMultiEntity接口的组件。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Entity> ForEachMultiEntity()
        {
            if (_multi == null)
            {
                yield break;
            }
            
            foreach (var (_, supportedMultiEntity) in _multi)
            {
                yield return (Entity)supportedMultiEntity;
            }
        }
        
        /// <summary>
        /// 获取一个 IEnumerable，用于遍历当前实体上挂载的普通组件。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Entity> ForEachEntity()
        {
            if (_tree == null)
            {
                yield break;
            }
            
            foreach (var (_, entity) in _tree)
            {
                yield return entity;
            }
        }

        #endregion

        #region GetComponent
        /// <summary>
        /// 获取当前实体上的一个指定类型的组件实体。
        /// </summary>
        /// <typeparam name="T">要获取的组件类型。</typeparam>
        /// <returns>找到的组件实体，如果不存在则为 null。</returns>
        public T GetComponent<T>() where T : Entity, new()
        {
            return GetComponent(typeof(T)) as T;
        }

        /// <summary>
        /// 获取当前实体上的一个指定类型的组件实体。
        /// </summary>
        /// <param name="componentType">要获取的组件类型。</param>
        /// <returns>找到的组件实体，如果不存在则为 null。</returns>
        public Entity GetComponent(Type componentType)
        {
            if (_tree == null)
            {
                return default;
            }

            return _tree.TryGetValue(componentType, out var component) ? component : default;
        }

        /// <summary>
        /// 获取当前实体上的一个指定类型的多实体组件。
        /// </summary>
        /// <typeparam name="T">要获取的多实体组件类型。</typeparam>
        /// <param name="id">多实体组件的ID。</param>
        /// <returns>找到的多实体组件，如果不存在则为 null。</returns>
        public T GetComponent<T>(long id) where T : ISupportedMultiEntity, new()
        {
            if (_multi == null)
            {
                return default;
            }

            return _multi.TryGetValue(id, out var entity) ? (T) entity : default;
        }

        #endregion

        #region RemoveComponent
        /// <summary>
        /// 从当前实体上移除一个指定类型的组件。
        /// </summary>
        /// <typeparam name="T">要移除的组件类型。</typeparam>
        /// <param name="isDispose">是否同时释放被移除的组件。</param>
        public void RemoveComponent<T>(bool isDispose = true) where T : Entity, new()
        {
            if (_tree == null || !_tree.TryGetValue(typeof(T), out var component))
            {
                return;
            }

            RemoveComponent(component, isDispose);
        }

        /// <summary>
        /// 从当前实体上移除一个指定类型的多实体组件。
        /// </summary>
        /// <typeparam name="T">要移除的多实体组件类型。</typeparam>
        /// <param name="id">要移除的多实体组件的ID。</param>
        /// <param name="isDispose">是否同时释放被移除的组件。</param>
        public void RemoveComponent<T>(long id, bool isDispose = true) where T : ISupportedMultiEntity, new()
        {
            if (_multi == null || !_multi.TryGetValue(id, out var component))
            {
                return;
            }

            RemoveComponent((Entity)component, isDispose);
        }

        /// <summary>
        /// 从当前实体上移除一个指定的组件实体。
        /// </summary>
        /// <param name="component">要移除的组件实体。</param>
        /// <param name="isDispose">是否同时释放被移除的组件。</param>
        public void RemoveComponent(Entity component, bool isDispose = true)
        {
            if (this == component)
            {
                return;
            }
            
            if (component is ISupportedMultiEntity)
            {
                if (_multi != null)
                {
#if FANTASY_NET
                    if (component is ISupportedDataBase && _multiDb != null)
                    {
                        _multiDb.Remove(component);

                        if (_multiDb.Count == 0)
                        {
                            _multiDb.Dispose();
                            _multiDb = null;
                        }
                    }
#endif
                    _multi.Remove(component.Id);

                    if (_multi.Count == 0)
                    {
                        _multi.Dispose();
                        _multi = null;
                    }
                }
            }
            else if (_tree != null)
            {
#if FANTASY_NET
                if (component is ISupportedDataBase && _treeDb != null)
                {
                    _treeDb.Remove(component);
            
                    if (_treeDb.Count == 0)
                    {
                        _treeDb.Dispose();
                        _treeDb = null;
                    }
                }
#endif
                _tree.Remove(component.GetType());
                
                if (_tree.Count == 0)
                {
                    _tree.Dispose();
                    _tree = null;
                }
            }

            if (isDispose)
            {
                component.Dispose();
            }
        }

        #endregion

        #region Deserialize
        /// <summary>
        /// 从序列化数据中恢复当前实体的状态，并将其添加到指定的场景中。
        /// </summary>
        /// <param name="scene">要添加到的场景。</param>
        /// <param name="resetId">是否重置实体的ID。</param>
        public void Deserialize(Scene scene, bool resetId = false)
        {
            if (RuntimeId != 0)
            {
                return;
            }

            try
            {
                Scene = scene;
#if FANTASY_NET
                RuntimeId = IdFactory.NextEntityId(scene.LocationId);
#else
                RuntimeId = IdFactory.NextRunTimeId();
#endif
                if (resetId)
                {
                    Id = RuntimeId;
                }

                Entities.Add(RuntimeId, this);

                if (_treeDb != null && _treeDb.Count > 0)
                {
                    _tree = DictionaryPool<Type, Entity>.Create();
                    foreach (var entity in _treeDb)
                    {
                        entity.Parent = this;
                        _tree.Add(entity.GetType(), entity);
                        entity.Deserialize(scene, resetId);
                    }
                }

                if (_multiDb != null && _multiDb.Count > 0)
                {
                    _multi = DictionaryPool<long, ISupportedMultiEntity>.Create();
                    foreach (var entity in _multiDb)
                    {
                        entity.Parent = this;
                        entity.Deserialize(scene, resetId);
                        _multi.Add(entity.Id, (ISupportedMultiEntity)entity);
                    }
                }
            }
            catch (Exception e)
            {
                if (RuntimeId != 0)
                {
                    Entities.Remove(RuntimeId);
                }

                Log.Error(e);
            }
        }

        #endregion

        #region Clone
        /// <summary>
        /// 克隆当前实体，并返回一个新的实体对象，新对象将具有相同的状态和组件。
        /// </summary>
        /// <returns>克隆生成的实体。</returns>
        public Entity Clone()
        {
#if FANTASY_NET
            var entity = MongoHelper.Instance.Clone(this);
            entity.Deserialize(Scene, true);
            return entity;
#elif FANTASY_UNITY
            var entity = ProtoBufHelper.Clone(this);
            entity.Deserialize(Scene, true);
            return entity;
#endif
        }

        #endregion

        #region Dispose
        /// <summary>
        /// 释放当前实体及其所有组件。如果实体已释放，则不执行任何操作。
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            var runtimeId = RuntimeId;
            RuntimeId = 0;

            if (_tree != null)
            {
                foreach (var (_, entity) in _tree)
                {
                    entity.Dispose();
                }

                _tree.Dispose();
                _tree = null;
            }
            
            if (_multi != null)
            {
                foreach (var (_, entity) in _multi)
                {
                    entity.Dispose();
                }
                
                _multi.Dispose();
                _multi = null;
            }
            
#if FANTASY_NET
            if (_treeDb != null)
            {
                foreach (var entity in _treeDb)
                {
                    entity.Dispose();
                }
                
                _treeDb.Dispose();
                _treeDb = null;
            }
            
            if (_multiDb != null)
            {
                foreach (var entity in _multiDb)
                {
                    entity.Dispose();
                }
                
                _multiDb.Dispose();
                _multiDb = null;
            }
#endif
            EntitiesSystem.Instance?.Destroy(this);

            if (Parent != null && Parent != this && !Parent.IsDisposed)
            {
                Parent.RemoveComponent(this, false);
                Parent = null;
            }

            Entities.Remove(runtimeId);
            Scene = null;
            Return(this);
        }

        #endregion
    }
}