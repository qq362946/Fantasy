using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fantasy.Entitas.Interface;
using Fantasy.Pool;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using ProtoBuf;

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

namespace Fantasy.Entitas
{
    /// <summary>
    /// 用来表示一个Entity
    /// </summary>
    public interface IEntity : IDisposable, IPool { }

    /// <summary>
    /// Entity的抽象类，任何Entity必须继承这个接口才可以使用
    /// </summary>
    public abstract partial class Entity : IEntity
    {
        #region Members

        /// <summary>
        /// 获取一个值，表示实体是否支持对象池。
        /// </summary>
        [BsonIgnore] 
        [JsonIgnore] 
        [ProtoIgnore]
        [IgnoreDataMember]
        private bool _isPool;
        /// <summary>
        /// 实体的Id
        /// </summary>
        [BsonId]
        [BsonElement]
        [BsonIgnoreIfDefault]
        [BsonDefaultValue(0L)]
        public long Id { get; protected set; }
        /// <summary>
        /// 实体的RunTimeId，其他系统可以通过这个Id发送Route消息，这个Id也可以理解为RouteId
        /// </summary>
        [BsonIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public long RuntimeId { get; protected set; }
        /// <summary>
        /// 当前实体是否已经被销毁
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public bool IsDisposed => RuntimeId == 0;
        /// <summary>
        /// 当前实体所归属的Scene
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public Scene Scene { get; protected set; }
        /// <summary>
        /// 实体的父实体
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public Entity Parent { get; protected set; }
        /// <summary>
        /// 实体的真实Type
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public Type Type { get; protected set; }
#if FANTASY_NET
        [BsonElement("t")] [BsonIgnoreIfNull] private EntityList<Entity> _treeDb;
        [BsonElement("m")] [BsonIgnoreIfNull] private EntityList<Entity> _multiDb;
#endif
        [BsonIgnore] [IgnoreDataMember] [ProtoIgnore] private EntitySortedDictionary<long, Entity> _tree;
        [BsonIgnore] [IgnoreDataMember] [ProtoIgnore] private EntitySortedDictionary<long, Entity> _multi;
        
        /// <summary>
        /// 获得父Entity
        /// </summary>
        /// <typeparam name="T">父实体的泛型类型</typeparam>
        /// <returns></returns>
        public T GetParent<T>() where T : Entity, new()
        {
            return Parent as T;
        }

        #endregion

        #region Create

        /// <summary>
        /// 创建一个实体
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="type">实体的Type</param>
        /// <param name="isPool">是否从对象池创建，如果选择的是，销毁的时候同样会进入对象池</param>
        /// <param name="isRunEvent">是否执行实体事件</param>
        /// <returns></returns>
        public static Entity Create(Scene scene, Type type, bool isPool, bool isRunEvent)
        {
            return Create(scene, type, scene.EntityIdFactory.Create, isPool, isRunEvent);
        }

        /// <summary>
        /// 创建一个实体
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="type">实体的Type</param>
        /// <param name="id">指定实体的Id</param>
        /// <param name="isPool">是否从对象池创建，如果选择的是，销毁的时候同样会进入对象池</param>
        /// <param name="isRunEvent">是否执行实体事件</param>
        /// <returns></returns>
        public static Entity Create(Scene scene, Type type, long id, bool isPool, bool isRunEvent)
        {
            if (!typeof(Entity).IsAssignableFrom(type))
            {
                throw new NotSupportedException($"{type.FullName} Type:{type.FullName} must inherit from Entity");
            }
            
            Entity entity = null;
            
            if (isPool)
            {
                entity = (Entity)scene.EntityPool.Rent(type);
            }
            else
            {
                if (!scene.TypeInstance.TryGetValue(type, out var createInstance))
                {
                    createInstance = CreateInstance.CreateIPool(type);
                    scene.TypeInstance[type] = createInstance;
                }

                entity = (Entity)createInstance();
            }
            
            entity.Scene = scene;
            entity.Type = type;
            entity.SetIsPool(isPool);
            entity.Id = id;
            entity.RuntimeId = scene.RuntimeIdFactory.Create;
            scene.AddEntity(entity);
            
            if (isRunEvent)
            {
                scene.EntityComponent.Awake(entity);
                scene.EntityComponent.StartUpdate(entity);
            }
            
            return entity;
        }
        
        /// <summary>
        /// 创建一个实体
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="isPool">是否从对象池创建，如果选择的是，销毁的时候同样会进入对象池</param>
        /// <param name="isRunEvent">是否执行实体事件</param>
        /// <typeparam name="T">要创建的实体泛型类型</typeparam>
        /// <returns></returns>
        public static T Create<T>(Scene scene, bool isPool, bool isRunEvent) where T : Entity, new()
        {
            return Create<T>(scene, scene.EntityIdFactory.Create, isPool, isRunEvent);
        }
        
        /// <summary>
        /// 创建一个实体
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="id">指定实体的Id</param>
        /// <param name="isPool">是否从对象池创建，如果选择的是，销毁的时候同样会进入对象池</param>
        /// <param name="isRunEvent">是否执行实体事件</param>
        /// <typeparam name="T">要创建的实体泛型类型</typeparam>
        /// <returns></returns>
        public static T Create<T>(Scene scene, long id, bool isPool, bool isRunEvent) where T : Entity, new()
        {
            var entity = isPool ? scene.EntityPool.Rent<T>() : new T();
            entity.Scene = scene;
            entity.Type = typeof(T);
            entity.SetIsPool(isPool);
            entity.Id = id;
            entity.RuntimeId = scene.RuntimeIdFactory.Create;
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

        /// <summary>
        /// 添加一个组件到当前实体上
        /// </summary>
        /// <param name="isPool">是否从对象池里创建</param>
        /// <typeparam name="T">要添加组件的泛型类型</typeparam>
        /// <returns>返回添加到实体上组件的实例</returns>
        public T AddComponent<T>(bool isPool = true) where T : Entity, new()
        {
            var id = SupportedMultiEntityChecker<T>.IsSupported ? Scene.EntityIdFactory.Create : Id;
            var entity = Create<T>(Scene, id, isPool, false);
            AddComponent(entity);
            Scene.EntityComponent.Awake(entity);
            Scene.EntityComponent.StartUpdate(entity);
            return entity;
        }

        /// <summary>
        /// 添加一个组件到当前实体上
        /// </summary>
        /// <param name="id">要添加组件的Id</param>
        /// <param name="isPool">是否从对象池里创建</param>
        /// <typeparam name="T">要添加组件的泛型类型</typeparam>
        /// <returns>返回添加到实体上组件的实例</returns>
        public T AddComponent<T>(long id, bool isPool = true) where T : Entity, new()
        {
            var entity = Create<T>(Scene, id, isPool, false);
            AddComponent(entity);
            Scene.EntityComponent.Awake(entity);
            Scene.EntityComponent.StartUpdate(entity);
            return entity;
        }

        /// <summary>
        /// 添加一个组件到当前实体上
        /// </summary>
        /// <param name="component">要添加的实体实例</param>
        public void AddComponent(Entity component)
        {
            if (this == component)
            {
                Log.Error("Cannot add oneself to one's own components");
                return;
            }

            if (component.IsDisposed)
            {
                Log.Error($"component is Disposed {component.Type.FullName}");
                return;
            }

            var type = component.Type;
            component.Parent?.RemoveComponent(component, false);

            if (component is ISupportedMultiEntity)
            {
                _multi ??= Scene.EntitySortedDictionaryPool.Rent();
                _multi.Add(component.Id, component);
#if FANTASY_NET
                if (component is ISupportedDataBase)
                {
                    _multiDb ??= Scene.EntityListPool.Rent();
                    _multiDb.Add(component);
                }
#endif
            }
            else
            {
#if FANTASY_NET
                if (component is ISupportedSingleCollection && component.Id != Id)
                {
                    Log.Error($"component type :{type.FullName} for implementing ISupportedSingleCollection, it is required that the Id must be the same as the parent");
                }
#endif
                var typeHashCode = Scene.EntityComponent.GetHashCode(type);;
                
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
                if (component is ISupportedDataBase)
                {
                    _treeDb ??= Scene.EntityListPool.Rent();
                    _treeDb.Add(component);
                } 
#endif
            }
            
            component.Parent = this;
            component.Scene = Scene;
        }

        /// <summary>
        /// 添加一个组件到当前实体上
        /// </summary>
        /// <param name="component">要添加的实体实例</param>
        /// <typeparam name="T">要添加组件的泛型类型</typeparam>
        public void AddComponent<T>(T component) where T : Entity
        {
            var type = typeof(T);

            if (type == typeof(Entity))
            {
                Log.Error("Cannot add a generic Entity type as a component. Specify a more specific type.");
                return;
            }
            
            if (this == component)
            {
                Log.Error("Cannot add oneself to one's own components");
                return;
            }

            if (component.IsDisposed)
            {
                Log.Error($"component is Disposed {type.FullName}");
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
                    Log.Error($"component type :{type.FullName} for implementing ISupportedSingleCollection, it is required that the Id must be the same as the parent");
                }
#endif
                var typeHashCode = Scene.EntityComponent.GetHashCode(type);
                
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

        /// <summary>
        ///  添加一个组件到当前实体上
        /// </summary>
        /// <param name="type">组件的类型</param>
        /// <param name="isPool">是否在对象池创建</param>
        /// <returns></returns>
        public Entity AddComponent(Type type, bool isPool = true)
        {
            var id = typeof(ISupportedMultiEntity).IsAssignableFrom(type) ? Scene.EntityIdFactory.Create : Id;
            var entity = Entity.Create(Scene, type, id, isPool, false);
            AddComponent(entity);
            Scene.EntityComponent.Awake(entity);
            Scene.EntityComponent.StartUpdate(entity);
            return entity;
        }

        #endregion

        #region HasComponent

        /// <summary>
        /// 当前实体上是否有指定类型的组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasComponent<T>() where T : Entity, new()
        {
            return HasComponent(typeof(T));
        }

        /// <summary>
        /// 当前实体上是否有指定类型的组件
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool HasComponent(Type type)
        {
            if (_tree == null)
            {
                return false;
            }
            
            return _tree.ContainsKey(Scene.EntityComponent.GetHashCode(type));
        }

        /// <summary>
        /// 当前实体上是否有指定类型的组件
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasComponent<T>(long id) where T : Entity, ISupportedMultiEntity, new()
        {
            if (_multi == null)
            {
                return false;
            }

            return _multi.ContainsKey(id);
        }

        #endregion

        #region GetComponent

        /// <summary>
        /// 当前实体上查找一个字实体
        /// </summary>
        /// <typeparam name="T">要查找实体泛型类型</typeparam>
        /// <returns>查找的实体实例</returns>
        public T GetComponent<T>() where T : Entity, new()
        {
            if (_tree == null)
            {
                return null;
            }
            
            var typeHashCode = Scene.EntityComponent.GetHashCode(typeof(T));
            return _tree.TryGetValue(typeHashCode, out var component) ? (T)component : null;
        }

        /// <summary>
        /// 当前实体上查找一个字实体
        /// </summary>
        /// <param name="type">要查找实体类型</param>
        /// <returns>查找的实体实例</returns>
        public Entity GetComponent(Type type)
        {
            if (_tree == null)
            {
                return null;
            }
            
            var typeHashCode = Scene.EntityComponent.GetHashCode(type);
            return _tree.TryGetValue(typeHashCode, out var component) ? component : null;
        }

        /// <summary>
        /// 当前实体上查找一个字实体
        /// </summary>
        /// <param name="id">要查找实体的Id</param>
        /// <typeparam name="T">要查找实体泛型类型</typeparam>
        /// <returns>查找的实体实例</returns>
        public T GetComponent<T>(long id) where T : Entity, ISupportedMultiEntity, new()
        {
            if (_multi == null)
            {
                return default;
            }

            return _multi.TryGetValue(id, out var entity) ? (T)entity : default;
        }

        /// <summary>
        /// 当前实体上查找一个字实体，如果没有就创建一个新的并添加到当前实体上
        /// </summary>
        /// <param name="isPool">是否从对象池创建</param>
        /// <typeparam name="T">要查找或添加实体泛型类型</typeparam>
        /// <returns>查找的实体实例</returns>
        public T GetOrAddComponent<T>(bool isPool = true) where T : Entity, new()
        {
            return GetComponent<T>() ?? AddComponent<T>(isPool);
        }

        #endregion

        #region RemoveComponent
        
        /// <summary>
        /// 当前实体下删除一个实体
        /// </summary>
        /// <param name="isDispose">是否执行删除实体的Dispose方法</param>
        /// <typeparam name="T">实体的泛型类型</typeparam>
        /// <exception cref="NotSupportedException"></exception>
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
            var typeHashCode = Scene.EntityComponent.GetHashCode(type);
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

        /// <summary>
        /// 当前实体下删除一个实体
        /// </summary>
        /// <param name="id">要删除的实体Id</param>
        /// <param name="isDispose">是否执行删除实体的Dispose方法</param>
        /// <typeparam name="T">实体的泛型类型</typeparam>
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

        /// <summary>
        /// 当前实体下删除一个实体
        /// </summary>
        /// <param name="component">要删除的实体实例</param>
        /// <param name="isDispose">是否执行删除实体的Dispose方法</param>
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
                    if (!_multi.ContainsKey(component.Id))
                    {
                        return;
                    }
#if FANTASY_NET
                    if (component is ISupportedDataBase)
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
                var typeHashCode = Scene.EntityComponent.GetHashCode(component.Type);
                if (!_tree.ContainsKey(typeHashCode))
                {
                    return;
                }
#if FANTASY_NET
                if (_treeDb != null && component is ISupportedDataBase)
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

        /// <summary>
        /// 当前实体下删除一个实体
        /// </summary>
        /// <param name="component">要删除的实体实例</param>
        /// <param name="isDispose">是否执行删除实体的Dispose方法</param>
        /// <typeparam name="T">实体的泛型类型</typeparam>
        public void RemoveComponent<T>(T component, bool isDispose = true) where T : Entity
        {
            if (this == component)
            {
                return;
            }

            if (typeof(T) == typeof(Entity))
            {
                Log.Error("Cannot remove a generic Entity type as a component. Specify a more specific type.");
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
                var typeHashCode = Scene.EntityComponent.GetHashCode(typeof(T));
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

        /// <summary>
        /// 反序列化当前实体，因为在数据库加载过来的或通过协议传送过来的实体并没有跟当前Scene做关联。
        /// 所以必须要执行一下这个反序列化的方法才可以使用。
        /// </summary>
        /// <param name="scene">Scene</param>
        /// <param name="resetId">是否是重新生成实体的Id,如果是数据库加载过来的一般是不需要的</param>
        public void Deserialize(Scene scene, bool resetId = false)
        {
            if (RuntimeId != 0)
            {
                return;
            }

            try
            {
                Scene = scene;
                Type ??= GetType();
                RuntimeId = Scene.RuntimeIdFactory.Create;
                if (resetId)
                {
                    Id = RuntimeId;
                }
#if FANTASY_NET
                if (_treeDb != null && _treeDb.Count > 0)
                {
                    _tree = Scene.EntitySortedDictionaryPool.Rent();
                    foreach (var entity in _treeDb)
                    {
                        entity.Parent = this;
                        entity.Type = entity.GetType();
                        var typeHashCode = Scene.EntityComponent.GetHashCode(entity.Type);
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
                scene.AddEntity(this);
                scene.EntityComponent.Deserialize(this);
            }
            catch (Exception e)
            {
                if (RuntimeId != 0)
                {
                    scene.RemoveEntity(RuntimeId);
                }

                Log.Error(e);
            }
        }

        #endregion

        #region ForEach
#if FANTASY_NET
        /// <summary>
        /// 查询当前实体下支持数据库分表存储实体
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
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
        /// 查询当前实体下支持传送实体
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
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
        /// <summary>
        /// 查询当前实体下的实现了ISupportedMultiEntity接口的实体
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
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
        /// <summary>
        /// 查找当前实体下的所有实体，不包括实现ISupportedMultiEntity接口的实体
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
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

        #region Dispose

        /// <summary>
        /// 销毁当前实体，销毁后会自动销毁当前实体下的所有实体。
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            var scene = Scene;
            var runTimeId = RuntimeId;
            RuntimeId = 0;
            
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

            if (IsPool())
            {
                scene.EntityPool.Return(Type, this);
            }

            Type = null;
        }

        #endregion

        #region Pool

        /// <summary>
        /// 获取一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <returns></returns>
        public bool IsPool()
        {
            return _isPool;
        }

        /// <summary>
        /// 设置一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <param name="isPool"></param>
        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;
        }

        #endregion
    }
}