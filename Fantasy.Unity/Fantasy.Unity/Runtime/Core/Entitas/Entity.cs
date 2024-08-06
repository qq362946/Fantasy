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
// ReSharper disable CheckNamespace
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy
{
    public abstract class Entity: IDisposable, IPool
    {
        #region Members
        /// <summary>
        /// 获取一个值，表示实体是否支持对象池。
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        public bool IsPool { get; set; }
        [BsonId]
        [BsonElement]
        [BsonIgnoreIfDefault]
        [BsonDefaultValue(0L)]
        public long Id { get; protected set; }
        [BsonIgnore]
        [IgnoreDataMember]
        public long RunTimeId { get; protected set; }
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        public bool IsDisposed => RunTimeId == 0;
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        public Scene Scene { get; protected set; }
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        public Entity Parent { get; protected set; }
#if FANTASY_NET
        [BsonElement("t")] [BsonIgnoreIfNull] private EntityList<Entity> _treeDb;
        [BsonElement("m")] [BsonIgnoreIfNull] private EntityList<Entity> _multiDb;
#endif
        [BsonIgnore] [IgnoreDataMember] private EntitySortedDictionary<long, Entity> _tree;
        [BsonIgnore] [IgnoreDataMember] private EntitySortedDictionary<long, Entity> _multi;
        
        public T GetParent<T>() where T : Entity, new()
        {
            return Parent as T;
        }

        #endregion

        #region Create
        
        public static T Create<T>(Scene scene, bool isPool, bool isRunEvent) where T : Entity, new()
        {
            var entity = isPool ? scene.EntityPool.Rent<T>() : new T();
            entity.Scene = scene;
            entity.IsPool = isPool;
            entity.Id = scene.EntityIdFactory.Create;
            entity.RunTimeId = scene.RuntimeIdFactory.Create;
            scene.AddEntity(entity);
            
            if (isRunEvent)
            {
                scene.EntityComponent.Awake(entity);
                scene.EntityComponent.StartUpdate(entity);
            }
            
            return entity;
        }

        public static T Create<T>(Scene scene, long id, bool isPool, bool isRunEvent) where T : Entity, new()
        {
            var entity = isPool ? scene.EntityPool.Rent<T>() : new T();
            entity.Scene = scene;
            entity.IsPool = isPool;
            entity.Id = id;
            entity.RunTimeId = scene.RuntimeIdFactory.Create;
            scene.AddEntity(entity);
            
            if (isRunEvent)
            {
                scene.EntityComponent.Awake(entity);
                scene.EntityComponent.StartUpdate(entity);
            }

            return entity;
        }

        #endregion
        
        #region AddComponent

        public T AddComponent<T>(bool isPool = true) where T : Entity, new()
        {
            var entity = Create<T>(Scene, Id, isPool, false);
            AddComponent(entity);
            Scene.EntityComponent.Awake(entity);
            Scene.EntityComponent.StartUpdate(entity);
            return entity;
        }

        public T AddComponent<T>(long id, bool isPool = true) where T : Entity, new()
        {
            var entity = Create<T>(Scene, id, isPool, false);
            AddComponent(entity);
            Scene.EntityComponent.Awake(entity);
            Scene.EntityComponent.StartUpdate(entity);
            return entity;
        }

        public void AddComponent<T>(T component) where T : Entity
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
            
            component.Parent?.RemoveComponent(component, false);
            
            if (SupportedMultiEntityChecker<T>.IsSupported)
            {
                _multi ??= Scene.EntitySortedDictionaryPool.Rent();
                _multi.Add(component.Id, component);
#if FANTASY_NET
                if (SupportedDataBaseChecker<T>.IsSupported)
                {
                    _multiDb ??= Scene.EntityListPool.Rent();
                    _multiDb.Add(component);
                }
#endif
            }
            else
            {
#if FANTASY_NET
                if (SupportedSingleCollectionChecker<T>.IsSupported && component.Id != Id)
                {
                    Log.Error($"component type :{component.GetType().FullName} for implementing ISupportedSingleCollection, it is required that the Id must be the same as the parent");
                }
#endif
                var type = typeof(T);
                var typeHashCode = type.TypeHandle.Value.ToInt64();
                
                if (_tree == null)
                {
                    _tree = Scene.EntitySortedDictionaryPool.Rent();
                }
                else if (_tree.ContainsKey(typeHashCode))
                {
                    Log.Error($"type:{type.FullName} If you want to add multiple components of the same type, please implement IMultiEntity");
                    return;
                }
                
                _tree.Add(typeHashCode, component);
#if FANTASY_NET
                if (SupportedDataBaseChecker<T>.IsSupported)
                {
                    _treeDb ??= Scene.EntityListPool.Rent();
                    _treeDb.Add(component);
                } 
#endif
            }
            
            component.Parent = this;
            component.Scene = Scene;
        }

        #endregion

        #region GetComponent

        public T GetComponent<T>() where T : Entity, new()
        {
            return _tree.TryGetValue(typeof(T).TypeHandle.Value.ToInt64(), out var component) ? (T)component : null;
        }

        public Entity GetComponent(Type type)
        {
            return _tree.TryGetValue(type.TypeHandle.Value.ToInt64(), out var component) ? component : null;
        }

        public T GetComponent<T>(long id) where T : Entity, ISupportedMultiEntity, new()
        {
            if (_multi == null)
            {
                return default;
            }

            return _multi.TryGetValue(id, out var entity) ? (T)entity : default;
        }

        public T GetOrAddComponent<T>(bool isPool = true) where T : Entity, new()
        {
            return GetComponent<T>() ?? AddComponent<T>(isPool);
        }

        #endregion

        #region RemoveComponent

        public void RemoveComponent<T>(bool isDispose = true) where T : Entity, new()
        {
            if (SupportedMultiEntityChecker<T>.IsSupported)
            {
                throw new NotSupportedException($"{typeof(T).FullName} message:Cannot delete components that implement the ISupportedMultiEntity interface");
            }
            
            if (_tree == null)
            {
                return;
            }
            
            var type = typeof(T);
            var typeHashCode = type.TypeHandle.Value.ToInt64();
            if (!_tree.TryGetValue(typeHashCode, out var component))
            {
                return;
            }
#if FANTASY_NET
            if (_treeDb != null && SupportedDataBaseChecker<T>.IsSupported)
            {
                _treeDb.Remove(component);

                if (_treeDb.Count == 0)
                {
                    Scene.EntityListPool.Return(_treeDb);
                    _treeDb = null;
                }
            }
#endif
            _tree.Remove(typeHashCode);

            if (_tree.Count == 0)
            {
                Scene.EntitySortedDictionaryPool.Return(_tree);
                _tree = null;
            }
            
            if (isDispose)
            {
                component.Dispose();
            }
        }

        public void RemoveComponent<T>(long id, bool isDispose = true) where T : Entity, ISupportedMultiEntity, new()
        {
            if (_multi == null)
            {
                return;
            }

            if (!_multi.TryGetValue(id, out var component))
            {
                return;
            }
#if FANTASY_NET
            if (SupportedDataBaseChecker<T>.IsSupported)
            {
                _multiDb.Remove(component);
                if (_multiDb.Count == 0)
                {
                    Scene.EntityListPool.Return(_multiDb);
                    _multiDb = null;
                }
            }
#endif
            _multi.Remove(component.Id);
            if (_multi.Count == 0)
            {
                Scene.EntitySortedDictionaryPool.Return(_multi);
                _multi = null;
            }
            
            if (isDispose)
            {
                component.Dispose();
            }
        }

        public void RemoveComponent<T>(T component, bool isDispose = true) where T : Entity
        {
            if (this == component)
            {
                return;
            }
            
            if (SupportedMultiEntityChecker<T>.IsSupported)
            {
                if (_multi != null)
                {
                    if (!_multi.ContainsKey(component.Id))
                    {
                        return;
                    }
#if FANTASY_NET
                    if (SupportedDataBaseChecker<T>.IsSupported)
                    {
                        _multiDb.Remove(component);
                        if (_multiDb.Count == 0)
                        {
                            Scene.EntityListPool.Return(_multiDb);
                            _multiDb = null;
                        }
                    }
#endif
                    _multi.Remove(component.Id);
                    if (_multi.Count == 0)
                    {
                        Scene.EntitySortedDictionaryPool.Return(_multi);
                        _multi = null;
                    }
                }
            }
            else if (_tree != null)
            {
                var typeHashCode = typeof(T).TypeHandle.Value.ToInt64();
                if (!_tree.ContainsKey(typeHashCode))
                {
                    return;
                }
#if FANTASY_NET
                if (_treeDb != null && SupportedDataBaseChecker<T>.IsSupported)
                {
                    _treeDb.Remove(component);

                    if (_treeDb.Count == 0)
                    {
                        Scene.EntityListPool.Return(_treeDb);
                        _treeDb = null;
                    }
                }
#endif
                _tree.Remove(typeHashCode);

                if (_tree.Count == 0)
                {
                    Scene.EntitySortedDictionaryPool.Return(_tree);
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

        public void Deserialize(Scene scene, bool resetId = false)
        {
            if (RunTimeId != 0)
            {
                return;
            }

            try
            {
                Scene = scene;
                RunTimeId = Scene.RuntimeIdFactory.Create;
                if (resetId)
                {
                    Id = RunTimeId;
                }
#if FANTASY_NET
                if (_treeDb != null && _treeDb.Count > 0)
                {
                    _tree = Scene.EntitySortedDictionaryPool.Rent();
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
                    _multi = Scene.EntitySortedDictionaryPool.Rent();
                    foreach (var entity in _multiDb)
                    {
                        entity.Parent = this;
                        entity.Deserialize(scene, resetId);
                        _multi.Add(entity.Id, entity);
                    }
                }
#endif
            }
            catch (Exception e)
            {
                if (RunTimeId != 0)
                {
                    scene.RemoveEntity(RunTimeId);
                }

                Log.Error(e);
            }
        }

        #endregion

        #region ForEach
#if FANTASY_NET
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
                        if (treeEntity is ISupportedTransfer)
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
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        public IEnumerable<Entity> ForEachMultiEntity
        {
            get
            {
                if (_multi == null)
                {
                    yield break;
                }

                foreach (var (_, supportedMultiEntity) in _multi)
                {
                    yield return supportedMultiEntity;
                }
            }
        }
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        public IEnumerable<Entity> ForEachEntity
        {
            get
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
        }
        #endregion

        #region Clone

        public Entity Clone()
        {
#if FANTASY_NET
            var entity = MongoHelper.Clone(this);
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

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            var scene = Scene;
            var runTimeId = RunTimeId;
            RunTimeId = 0;
            
            if (_tree != null)
            {
                foreach (var (_, entity) in _tree)
                {
                    entity.Dispose();
                }

                scene.EntitySortedDictionaryPool.Return(_tree);
                _tree = null;
            }
            
            if (_multi != null)
            {
                foreach (var (_, entity) in _multi)
                {
                    entity.Dispose();
                }

                scene.EntitySortedDictionaryPool.Return(_multi);
                _multi = null;
            }
#if FANTASY_NET
            if (_treeDb != null)
            {
                foreach (var entity in _treeDb)
                {
                    entity.Dispose();
                }

                scene.EntityListPool.Return(_treeDb);
                _treeDb = null;
            }
            
            if (_multiDb != null)
            {
                foreach (var entity in _multiDb)
                {
                    entity.Dispose();
                }

                scene.EntityListPool.Return(_multiDb);
                _multiDb = null;
            }
#endif
            scene.EntityComponent.Destroy(this);
            
            if (Parent != null && Parent != this && !Parent.IsDisposed)
            {
                Parent.RemoveComponent(this, false);
                Parent = null;
            }

            Id = 0;
            Scene = null;
            Parent = null;
            scene.RemoveEntity(runTimeId);

            if (IsPool)
            {
                scene.EntityPool.Return(this);
            }
        }

        #endregion
    }
}