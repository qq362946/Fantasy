using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable MergeIntoPattern
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
#pragma warning disable CS0169 // Field is never used

namespace Fantasy
{
    /// <summary>
    /// 实体基类，用于实体创建、回收、获取，和组件操作
    /// </summary>
    public abstract class Entity : IDisposable, IPool
    {
        #region Members

        /// <summary>
        /// 获取一个值，表示实体是否支持对象池。
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        public bool IsPool { get; set; }

        /// <summary>
        /// 获取或设置实体的唯一ID。
        /// </summary>
        [BsonId]
        [BsonElement]
        [BsonIgnoreIfDefault]
        [BsonDefaultValue(0L)]
        public long Id { get; protected set; }

        /// <summary>
        /// 获取实体的运行时ID。
        /// </summary>
        [BsonIgnore]
        [IgnoreDataMember]
        public long RuntimeId { get; protected set; }

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

        [BsonElement("t")] [BsonIgnoreIfNull] private ListPool<Entity> _treeDb;

        [BsonIgnore] [IgnoreDataMember] private SortedDictionaryPool<long, Entity> _tree;

        [BsonElement("m")] [BsonIgnoreIfNull] private ListPool<Entity> _multiDb;

        [BsonIgnore] [IgnoreDataMember] private SortedDictionaryPool<long, ISupportedMultiEntity> _multi;

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

        #region Create

        /// <summary>
        /// 在指定场景中创建一个具有指定ID的实体对象，并触发相关事件（可选）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="scene">要创建实体的场景。</param>
        /// <param name="id">要分配给实体的ID。</param>
        /// <param name="isPool">是否从对象池创建。</param>
        /// <param name="isRunEvent">是否触发相关事件。</param>
        /// <returns>创建的实体对象。</returns>
        public static T Create<T>(Scene scene, long id, bool isPool, bool isRunEvent = true) where T : Entity, new()
        {
            var entity = Create<T>(scene, id, IdFactory.NextEntityId(scene.ProcessId), isPool, isRunEvent);
            entity.Scene = scene;
            return entity;
        }
        
        /// <summary>
        /// 在指定场景中创建一个实体对象，并触发相关事件（可选）。
        /// </summary>
        /// <param name="scene">要创建实体的场景</param>
        /// <param name="isPool">是否从对象池创建。</param>
        /// <param name="isRunEvent">是否触发相关事件。</param>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <returns></returns>
        public static T Create<T>(Scene scene, bool isPool, bool isRunEvent = true) where T : Entity, new()
        {
#if FANTASY_NET
            var id = IdFactory.NextEntityId(scene.ProcessId);
#else
            var id = IdFactory.NextRunTimeId();
#endif
            var entity = Create<T>(scene, id, id, isPool, isRunEvent);
            entity.Scene = scene;
            return entity;
        }

        /// <summary>
        /// 在指定位置中创建一个实体对象，并可选择是否立即触发事件。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="scene">当前的Scene</param>
        /// <param name="id">要分配给实体的ID。</param>
        /// <param name="runtimeId">要分配给实体的运行时ID。</param>
        /// <param name="isPool">是否从对象池创建。</param>
        /// <param name="isRunEvent">是否立即触发实体事件。</param>
        /// <returns>创建的实体对象。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T Create<T>(Scene scene, long id, long runtimeId, bool isPool, bool isRunEvent = true) where T : Entity, new()
        {
            Entity entity;

            if (isPool)
            {
                entity = (Entity)scene.Pool.Rent(typeof(T));
            }
            else
            {
                entity = (Entity)Activator.CreateInstance(typeof(T));
            }

            entity.Id = id;
            entity.RuntimeId = runtimeId;
            entity.IsPool = isPool;
            scene.Entities.Add(entity.RuntimeId, entity);

            if (isRunEvent)
            {
                scene.EntityComponent.Awake(entity);
                scene.EntityComponent.StartUpdate(entity);
            }

            return (T)entity;
        }

        #endregion

        #region AddComponent

        /// <summary>
        /// 在当前实体上添加一个指定类型的组件，并立即触发组件事件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="isPool">是否从对象池创建。</param>
        /// <returns>创建的组件实体。</returns>
        public T AddComponent<T>(bool isPool = true) where T : Entity, new()
        {
            var entity = Create<T>(Scene, Id, isPool, false);
            AddComponent(entity);
            Scene.EntityComponent.Awake(entity);
            Scene.EntityComponent.StartUpdate(entity);
            return entity;
        }

        /// <summary>
        /// 在当前实体上添加一个指定类型的组件，并立即触发组件事件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="id">要分配给组件的ID。</param>
        /// <param name="isPool">是否从对象池创建。</param>
        /// <returns>创建的组件实体。</returns>
        public T AddComponent<T>(long id, bool isPool = true) where T : Entity, new()
        {
            var entity = Create<T>(Scene, id, isPool, false);
            AddComponent(entity);
            Scene.EntityComponent.Awake(entity);
            Scene.EntityComponent.StartUpdate(entity);
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
            var typeHashCode = type.TypeHandle.Value.ToInt64();
            
            component.Parent?.RemoveComponent(component, false);

            if (component is ISupportedMultiEntity multiEntity)
            {
                _multi ??= SortedDictionaryPool<long, ISupportedMultiEntity>.Create();
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
                    _tree = SortedDictionaryPool<long, Entity>.Create();
                }
                else if (_tree.ContainsKey(typeHashCode))
                {
                    Log.Error($"type:{type.FullName} If you want to add multiple components of the same type, please implement IMultiEntity");
                    return;
                }

                _tree.Add(typeHashCode, component);

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
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
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
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
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
            var typeHashCode = componentType.TypeHandle.Value.ToInt64();
            return _tree?.GetValueOrDefault(typeHashCode);
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

            return _multi.TryGetValue(id, out var entity) ? (T)entity : default;
        }

        /// <summary>
        /// 获取当前实体上的一个指定类型的组件实体。
        /// </summary>
        /// <param name="isNullAdd">如果没有查找到就创建一个新的并返回</param>
        /// <param name="isPool">是否从对象池创建。</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetComponent<T>(bool isNullAdd, bool isPool = true) where T : Entity, new()
        {
            var component = GetComponent<T>();

            if (component == null && isNullAdd)
            {
                return AddComponent<T>(isPool);
            }

            return component;
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
            var type = typeof(T);
            var typeHashCode = type.TypeHandle.Value.ToInt64();
            if (_tree == null || !_tree.TryGetValue(typeHashCode, out var component))
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
            
            var typeHashCode = component.GetType().TypeHandle.Value.ToInt64();

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
                _tree.Remove(typeHashCode);

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
                RuntimeId = IdFactory.NextEntityId(scene.ProcessId);
#else
                RuntimeId = IdFactory.NextRunTimeId();
#endif
                if (resetId)
                {
                    Id = RuntimeId;
                }

                scene.Entities.Add(RuntimeId, this);

                if (_treeDb != null && _treeDb.Count > 0)
                {
                    _tree = SortedDictionaryPool<long, Entity>.Create();
                    foreach (var entity in _treeDb)
                    {
                        entity.Parent = this;
                        var typeHashCode = entity.GetType().TypeHandle.Value.ToInt64();
                        _tree.Add(typeHashCode, entity);
                        entity.Deserialize(scene, resetId);
                    }
                }

                if (_multiDb != null && _multiDb.Count > 0)
                {
                    _multi = SortedDictionaryPool<long, ISupportedMultiEntity>.Create();
                    foreach (var entity in _multiDb)
                    {
                        entity.Parent = this;
                        entity.Deserialize(scene, resetId);
                        _multi.Add(entity.Id, (ISupportedMultiEntity)entity);
                    }
                }
                
                scene.EntityComponent.Deserialize(this);
            }
            catch (Exception e)
            {
                if (RuntimeId != 0)
                {
                    scene.Entities.Remove(RuntimeId);
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
            var entity = ProtoBuffHelper.Clone(this);
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

            var scene = Scene;
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
            scene.EntityComponent.Destroy(this);

            if (Parent != null && Parent != this && !Parent.IsDisposed)
            {
                Parent.RemoveComponent(this, false);
                Parent = null;
            }

            scene.Entities.Remove(runtimeId);
            Scene = null;
            
            if (IsPool)
            {
                IsPool = false;
                scene.Pool.Return(this);
            }
        }

        #endregion
    }
}

