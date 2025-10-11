// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

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

    internal struct CustomEntitySystemKey : IEquatable<CustomEntitySystemKey>
    {
        public int CustomEventType { get; }
        public Type EntityType { get; }
        public CustomEntitySystemKey(int customEventType, Type entityType)
        {
            CustomEventType = customEventType;
            EntityType = entityType;
        }
        public bool Equals(CustomEntitySystemKey other)
        {
            return CustomEventType == other.CustomEventType && EntityType == other.EntityType;
        }

        public override bool Equals(object obj)
        {
            return obj is CustomEntitySystemKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CustomEventType, EntityType);
        }
    }

    /// <summary>
    /// Entity管理组件
    /// </summary>
#if FANTASY_UNITY
    public sealed class EntityComponent : Entity, ISceneUpdate, ISceneLateUpdate, IAssembly
#else
    public sealed class EntityComponent : Entity, ISceneUpdate, IAssembly
#endif
    {
        private readonly OneToManyList<long, Type> _assemblyList = new();
        private readonly OneToManyList<long, Type> _assemblyHashCodes = new();

        private readonly Dictionary<Type, IAwakeSystem> _awakeSystems = new();
        private readonly Dictionary<Type, IUpdateSystem> _updateSystems = new();
        private readonly Dictionary<Type, IDestroySystem> _destroySystems = new();
        private readonly Dictionary<Type, IDeserializeSystem> _deserializeSystems = new();

        private readonly Dictionary<Type, Type> _genericAwakeSystemByEntityDefinition = new();
        private readonly Dictionary<Type, Type> _genericUpdateSystemByEntityDefinition = new();
        private readonly Dictionary<Type, Type> _genericDestroySystemByEntityDefinition = new();
        private readonly Dictionary<Type, Type> _genericDeserializeSystemByEntityDefinition = new();

        private readonly OneToManyList<long, CustomEntitySystemKey> _assemblyCustomSystemList = new();
        private readonly Dictionary<CustomEntitySystemKey, ICustomEntitySystem> _customEntitySystems = new();

        private readonly Dictionary<Type, long> _typeHashCodesFromFullName = new Dictionary<Type, long>();

        private readonly Queue<UpdateQueueInfo> _updateQueue = new Queue<UpdateQueueInfo>();
        private readonly Dictionary<long, UpdateQueueInfo> _updateQueueDic = new Dictionary<long, UpdateQueueInfo>();
#if FANTASY_UNITY
        private readonly Dictionary<Type, ILateUpdateSystem> _lateUpdateSystems = new();
        private readonly Dictionary<Type, Type> _genericLateUpdateSystemByEntityDefinition = new();
        private readonly Queue<UpdateQueueInfo> _lateUpdateQueue = new Queue<UpdateQueueInfo>();
        private readonly Dictionary<long, UpdateQueueInfo> _lateUpdateQueueDic = new Dictionary<long, UpdateQueueInfo>();
#endif

        internal async FTask<EntityComponent> Initialize()
        {
            await AssemblySystem.Register(this);
            return this;
        }

        #region Assembly

        public FTask Load(long assemblyIdentity)
        {
            var task = FTask.Create(false);
            Scene?.ThreadSynchronizationContext.Post(() =>
            {
                LoadInner(assemblyIdentity);
                task.SetResult();
            });
            return task;
        }

        public FTask ReLoad(long assemblyIdentity)
        {
            var task = FTask.Create(false);
            Scene?.ThreadSynchronizationContext.Post(() =>
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
            Scene?.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                task.SetResult();
            });
            return task;
        }

        private void LoadInner(long assemblyIdentity)
        {

            OneToManyList<Type, Type>? closedGenericsByDefinition = AssemblySystem.GetClosedGenericsByDefinition(assemblyIdentity);

            //注册预闭合泛型实体
            if (closedGenericsByDefinition != null)
            {
                foreach (KeyValuePair<Type, List<Type>> typeListKV in closedGenericsByDefinition)
                {
                    if (typeof(IEntity).IsAssignableFrom(typeListKV.Key))
                    {
                        foreach (var entityType in typeListKV.Value)
                        {
                            if (ProjectSettings.EntitySettings.TypeCodeMode == TypeCodeMode.FromFullName)
                                _typeHashCodesFromFullName.Add(entityType, HashCodeHelper.ComputeHash64(entityType.FullName));
                            _assemblyHashCodes.Add(assemblyIdentity, entityType);
                        }
                    }
                }
            }

            //注册实体
            foreach (var entityType in AssemblySystem.ForEach(assemblyIdentity, typeof(IEntity)))
            {
                if (entityType.IsGenericTypeDefinition)
                {
#if FANTASY_NET
                    if (closedGenericsByDefinition.ContainsKey(entityType) == false)
                    {
                        Log.Warning($"检测到泛型实体 ({entityType})在程序集{assemblyIdentity}当中从未进行任何闭合泛型预注册,请务必留意! 可能存在Awake时反射风险!");
                        Log.Warning($"Detected a generic entity ({entityType}) in assembly {assemblyIdentity} that has never been pre-registered with any closed generic types. Please be aware — this may cause reflection overhead during Awake!");
                    }
#endif
                    continue;
                }
                if (ProjectSettings.EntitySettings.TypeCodeMode == TypeCodeMode.FromFullName)
                    _typeHashCodesFromFullName.Add(entityType, HashCodeHelper.ComputeHash64(entityType.FullName));
                _assemblyHashCodes.Add(assemblyIdentity, entityType);
            }

            //注册Systems
            foreach (Type entitySystemType in AssemblySystem.ForEach(assemblyIdentity, typeof(IEntitySystem)))
            {
                if (entitySystemType.IsGenericTypeDefinition)
                {
                    if (!TryPreRegisterAllGenericEntitySystems(entitySystemType, closedGenericsByDefinition,
                       out Type entityDefinition, assemblyIdentity))
                    {
                        RegisterAsOpenGenericSystem(entityDefinition, entitySystemType);
                        continue;
                    }
                    RegisterAsOpenGenericSystem(entityDefinition, entitySystemType);
                }
                else
                {
                    object? system = Activator.CreateInstance(entitySystemType);
                    if (!RegisterSystemByInterfaceType(system, assemblyIdentity, entitySystemType))
                        return;
                }
            }

            //注册自定义System
            foreach (var customEntitySystemType in AssemblySystem.ForEach(assemblyIdentity, typeof(ICustomEntitySystem)))
            {
                if (customEntitySystemType.IsGenericType)
                {
                    if (!TryPreRegisterAllCustomGenericEntitySystems(customEntitySystemType,
                        closedGenericsByDefinition, out Type entityDefinition, assemblyIdentity
                        ))
                    {
                        RegisterAsOpenGenericSystem(entityDefinition, customEntitySystemType);
                        continue;
                    }
                    RegisterAsOpenGenericSystem(entityDefinition, customEntitySystemType);
                }
                else
                {
                    object system = Activator.CreateInstance(customEntitySystemType);
                    RegisterCustomSystemByBuiltKey(system, customEntitySystemType, assemblyIdentity);
                }
            }
        }

        private void OnUnLoadInner(long assemblyIdentity)
        {
            if (_assemblyHashCodes.TryGetValue(assemblyIdentity, out List<Type>? entityTypeList))
            {
                foreach (var type in entityTypeList)
                {
                    _typeHashCodesFromFullName.Remove(type);
                }

                _assemblyHashCodes.RemoveByKey(assemblyIdentity);
            }

            if (_assemblyList.TryGetValue(assemblyIdentity, out List<Type>? assemblyList))
            {
                foreach (var type in assemblyList)
                {
                    _awakeSystems.Remove(type);
                    _updateSystems.Remove(type);
                    _destroySystems.Remove(type);
#if FANTASY_UNITY
                    _lateUpdateSystems.Remove(type);
#endif
                    _deserializeSystems.Remove(type);

                    _genericAwakeSystemByEntityDefinition.Remove(type);
                    _genericUpdateSystemByEntityDefinition.Remove(type);
                    _genericDestroySystemByEntityDefinition.Remove(type);
#if FANTASY_UNITY
                    _genericLateUpdateSystemByEntityDefinition.Remove(type);
#endif
                    _genericDeserializeSystemByEntityDefinition.Remove(type);
                }

                _assemblyList.RemoveByKey(assemblyIdentity);
            }

            if (_assemblyCustomSystemList.TryGetValue(assemblyIdentity, out var customSystemAssembly))
            {
                foreach (var customEntitySystemKey in customSystemAssembly)
                {
                    _customEntitySystems.Remove(customEntitySystemKey);
                }

                _assemblyCustomSystemList.RemoveByKey(assemblyIdentity);
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
                if (entity.Type.IsGenericType == false) return;
                else
                {
                    //运行时实时反射构造泛型对应的System实例,一般来说 建议用ClosedGenericAttribute预注册,
                    // 但是也兼容: 

                    var definition = entity.Type.GetGenericTypeDefinition();

                    // 处理AwakeSystem
                    if (_genericAwakeSystemByEntityDefinition.TryGetValue(definition, out var genericAwakeSystemType))
                    {
                        var awakeBuilt = TryBuildClosedGenericSystemInstanceWithEntityType(genericAwakeSystemType, entity.Type);
                        if (awakeBuilt.HasValue && awakeBuilt.Value.Item1 is IAwakeSystem builtAwake)
                        {
                            awakeSystem = builtAwake;
                            _awakeSystems.Add(entity.Type, awakeSystem);
                        }
                    }

                    // 处理DeserializeSystem
                    if (_genericDeserializeSystemByEntityDefinition.TryGetValue(definition, out var genericDeserializeSystemType))
                    {
                        var deserializeBuilt = TryBuildClosedGenericSystemInstanceWithEntityType(genericDeserializeSystemType, entity.Type);
                        if (deserializeBuilt.HasValue && deserializeBuilt.Value.Item1 is IDeserializeSystem builtDeserialize)
                        {
                            _deserializeSystems.Add(entity.Type, builtDeserialize);
                        }
                    }

                    // 处理DestroySystem
                    if (_genericDestroySystemByEntityDefinition.TryGetValue(definition, out var genericDestroySystemType))
                    {
                        var destroyBuilt = TryBuildClosedGenericSystemInstanceWithEntityType(genericDestroySystemType, entity.Type);
                        if (destroyBuilt.HasValue && destroyBuilt.Value.Item1 is IDestroySystem builtDestroy)
                        {
                            _destroySystems.Add(entity.Type, builtDestroy);
                        }
                    }

                    // 处理UpdateSystem
                    if (_genericUpdateSystemByEntityDefinition.TryGetValue(definition, out var genericUpdateSystemType))
                    {
                        var updateBuilt = TryBuildClosedGenericSystemInstanceWithEntityType(genericUpdateSystemType, entity.Type);
                        if (updateBuilt.HasValue && updateBuilt.Value.Item1 is IUpdateSystem builtUpdate)
                        {
                            _updateSystems.Add(entity.Type, builtUpdate);
                        }
                    }

#if FANTASY_UNITY
                    // 处理LateUpdateSystem
                    if (_genericLateUpdateSystemByEntityDefinition.TryGetValue(definition, out var genericLateUpdateSystemType))
                    {
                        var lateUpdateBuilt = TryBuildClosedGenericSystemInstanceWithEntityType(genericLateUpdateSystemType, entity.Type);
                        if (lateUpdateBuilt.HasValue && lateUpdateBuilt.Value.Item1 is ILateUpdateSystem builtLateUpdate)
                        {
                            _lateUpdateSystems.Add(entity.Type, builtLateUpdate);
                        }
                    }
#endif

                    if (awakeSystem is null)
                        return;
                }
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

        #region CustomEvent

        public void CustomSystem(Entity entity, int customEventType)
        {
            var customEntitySystemKey = new CustomEntitySystemKey(customEventType, entity.Type);

            if (!_customEntitySystems.TryGetValue(customEntitySystemKey, out var system))
            {
                return;
            }

            try
            {
                system.Invoke(entity);
            }
            catch (Exception e)
            {
                Log.Error($"{entity.Type.FullName} CustomSystem Error {e}");
            }
        }

        #endregion

        #region Update

        /// <summary>
        /// 将实体加入Update队列，准备进行Update
        /// </summary>
        /// <param name="entity">实体对象</param>
        public void StartUpdate(Entity entity)
        {
            var type = entity.Type;
            var entityRuntimeId = entity.RuntimeId;

            if (!_updateSystems.ContainsKey(type))
            {
                return;
            }

            var updateQueueInfo = new UpdateQueueInfo(type, entityRuntimeId);
            _updateQueue.Enqueue(updateQueueInfo);
            _updateQueueDic.Add(entityRuntimeId, updateQueueInfo);
        }

        /// <summary>
        /// 停止实体Update
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
        /// 执行实体系统的Update
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

        #endregion

#if FANTASY_UNITY
        #region LateUpdate

        /// <summary>
        /// 将实体加入LateUpdate队列，准备进行LateUpdate
        /// </summary>
        /// <param name="entity">实体对象</param>
        public void StartLateUpdate(Entity entity)
        {
            var type = entity.Type;
            var entityRuntimeId = entity.RuntimeId;

            if (!_lateUpdateSystems.ContainsKey(type))
            {
                return;
            }

            var updateQueueInfo = new UpdateQueueInfo(type, entityRuntimeId);
            _lateUpdateQueue.Enqueue(updateQueueInfo);
            _lateUpdateQueueDic.Add(entityRuntimeId, updateQueueInfo);
        }

        /// <summary>
        /// 停止实体进行LateUpdate
        /// </summary>
        /// <param name="entity">实体对象</param>
        public void StopLateUpdate(Entity entity)
        {
            if (!_lateUpdateQueueDic.Remove(entity.RuntimeId, out var updateQueueInfo))
            {
                return;
            }

            updateQueueInfo.IsStop = true;
        }

        public void LateUpdate()
        {
            var lateUpdateQueue = _lateUpdateQueue.Count;

            while (lateUpdateQueue-- > 0)
            {
                var lateUpdateQueueStruct = _lateUpdateQueue.Dequeue();

                if (lateUpdateQueueStruct.IsStop)
                {
                    continue;
                }

                if (!_lateUpdateSystems.TryGetValue(lateUpdateQueueStruct.Type, out var lateUpdateSystem))
                {
                    continue;
                }

                var entity = Scene.GetEntity(lateUpdateQueueStruct.RunTimeId);

                if (entity == null || entity.IsDisposed)
                {
                    _lateUpdateQueueDic.Remove(lateUpdateQueueStruct.RunTimeId);
                    continue;
                }

                _lateUpdateQueue.Enqueue(lateUpdateQueueStruct);

                try
                {
                    lateUpdateSystem.Invoke(entity);
                }
                catch (Exception e)
                {
                    Log.Error($"{lateUpdateQueueStruct.Type.FullName} Update Error {e}");
                }
            }
        }
        #endregion
#endif

        #region Method      

        /// <summary>从泛型系统类型的基类中提取实体泛型定义</summary>
        /// <param name="genericSystemType">泛型系统类型（如XXXXDestroy）</param>
        /// <param name="entityGenericDefinition">输出的实体泛型定义</param>
        private void GetEntityGenericDefinitionFromSystem(Type genericSystemType, out Type entityGenericDefinition)
        {
            // 从泛型参数取实体泛型定义,
            // 比如 XXXXDestroy:XXXXSystem<XXXX>, 基类中第一个泛型参数XXXX即为所求实体泛型
            Type baseType = genericSystemType.BaseType;
            var arg = baseType.GetGenericArguments()[0];
            entityGenericDefinition = arg.GetGenericTypeDefinition();
        }

        /// <summary>
        /// 泛型实体闭合System预注册。
        /// 预注册成功返回 true
        /// 预注册失败返回 false
        /// </summary>
        /// <param name="genericSystemType">待构造的泛型System</param>
        /// <param name="preregisteredEntityGenericsByDefinition">预注册的实体泛型定义集合</param>
        /// <param name="entityGenericDefinition">实体泛型定义</param>
        /// <param name="assemblyIdentity">程序集Id</param>
        /// <returns>成功返回true 失败返回false</returns>
        private bool TryPreRegisterAllGenericEntitySystems(
            Type genericSystemType,
          OneToManyList<Type, Type>? preregisteredEntityGenericsByDefinition,
          out Type entityGenericDefinition,
          long assemblyIdentity
            )
        {
            // 1. 从泛型参数取实体泛型定义
            GetEntityGenericDefinitionFromSystem(genericSystemType, out entityGenericDefinition);

            // 2. 从closedGenericsByDefinition检测标签预注册
            if (preregisteredEntityGenericsByDefinition.ContainsKey(entityGenericDefinition) == false)
            {
                return false;
            }

            // 3. 处理每个闭合泛型类型, 强制要求必须全部成功, 才返回true
            bool isAllSuccessful = true;
            foreach (var preregisteredEntityType in preregisteredEntityGenericsByDefinition[entityGenericDefinition])
            {
                try
                {
                    (object, Type)? result = TryBuildClosedGenericSystemInstanceWithEntityType(genericSystemType, preregisteredEntityType);

                    if (!RegisterSystemByInterfaceType(result!.Value.Item1, assemblyIdentity, result!.Value.Item2)
                       || result == null)
                        isAllSuccessful = false;
                }
                catch (Exception ex)
                {
                    Log.Error($"处理闭合泛型({preregisteredEntityType})发生错误. (Error processing closed generic, msg: {ex.Message})");
                    isAllSuccessful = false;
                }
            }
            return isAllSuccessful;
        }

        /// <summary>
        /// 针对自定义的泛型实体System进行预注册
        /// </summary>
        /// <param name="customSystemType"></param>
        /// <param name="preregisteredEntityGenericsByDefinition"></param>
        /// <param name="entityGenericDefinition"></param>
        /// <param name="assemblyIdentity"></param>
        /// <returns></returns>
        private bool TryPreRegisterAllCustomGenericEntitySystems(
           Type customSystemType,
         OneToManyList<Type, Type>? preregisteredEntityGenericsByDefinition,
         out Type entityGenericDefinition,
         long assemblyIdentity
           )
        {
            // 1. 与TryPreRegisterAllGenericEntitySystems 相同
            GetEntityGenericDefinitionFromSystem(customSystemType, out entityGenericDefinition);

            // 2. 与TryPreRegisterAllGenericEntitySystems 相同
            if (preregisteredEntityGenericsByDefinition.ContainsKey(entityGenericDefinition) == false)
            {
                return false;
            }

            // 3. 与TryPreRegisterAllGenericEntitySystems 相似, 但是注册的是CustomSystem实例
            bool isAllSuccessful = true;
            foreach (var preregisteredEntityType in preregisteredEntityGenericsByDefinition[entityGenericDefinition])
            {
                try
                {
                    (object, Type)? result = TryBuildClosedGenericSystemInstanceWithEntityType(customSystemType, preregisteredEntityType);
                    if (result == null)
                        isAllSuccessful = false;
                    else
                        RegisterCustomSystemByBuiltKey(result.Value.Item1, result.Value.Item2, assemblyIdentity);
                }
                catch (Exception ex)
                {
                    Log.Error($"处理闭合泛型({preregisteredEntityType})发生错误. (Error processing closed generic, msg: {ex.Message})");
                    isAllSuccessful = false;
                }
            }
            return isAllSuccessful;
        }

        /// <summary>
        /// 根据闭合泛型实体, 构建对应的闭合泛型实体System的实例
        /// </summary>
        /// <param name="genericSystemType">待构造的泛型System</param>
        /// <param name="entityType">泛型实体的定义</param>
        /// <returns>返回一个System实例object, 和闭合的System泛型Type</returns>
        private (object, Type)? TryBuildClosedGenericSystemInstanceWithEntityType(
            Type genericSystemType, Type entityType)
        {
            try
            {
                if (genericSystemType is null || entityType is null)
                    return null;

                Type[] entityGenericArgs = entityType.GetGenericArguments();
                Type closedSystemType = genericSystemType.MakeGenericType(entityGenericArgs);

                Log.Debug($"(泛型System构造) 已从({entityType})构造闭合泛型System({closedSystemType})");
                Log.Debug($"(Generic-System Built) A closed System-Generic has been built, ({closedSystemType}) built from ({entityType})");

                object systemInstance = Activator.CreateInstance(closedSystemType)!;
                return (systemInstance, closedSystemType);
            }
            catch (Exception ex)
            {
                Log.Error($"处理闭合泛型({entityType})发生错误.");
                Log.Error($"Error processing closed generic, msg: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 注册开放式泛型实体System (与这一注册配套的是运行时构建)
        /// </summary>
        /// <param name="entityDefinition">实体泛型原始定义</param>
        /// <param name="openGenericSystem">开放式泛型System原始定义</param>
        private void RegisterAsOpenGenericSystem(Type entityDefinition, Type openGenericSystem)
        {
            // 按顺序取四种接口中第一个实现的
            Type? implementedInterface = new[]
            {
                            typeof(IAwakeSystem),
                            typeof(IUpdateSystem),
                            typeof(IDestroySystem),
                            typeof(IDeserializeSystem)
                        }.FirstOrDefault(iface => iface.IsAssignableFrom(openGenericSystem));

            if (implementedInterface is not null)
            {
                // 按顺序检查System实现的接口，匹配到第一个就处理
                if (implementedInterface == typeof(IAwakeSystem))
                {
                    // 键是实体泛型定义，值是泛型System定义
                    _genericAwakeSystemByEntityDefinition.Add(entityDefinition, openGenericSystem);
                }
                else if (implementedInterface == typeof(IUpdateSystem))
                {
                    _genericUpdateSystemByEntityDefinition.Add(entityDefinition, openGenericSystem);
                }
                else if (implementedInterface == typeof(IDestroySystem))
                {
                    _genericDestroySystemByEntityDefinition.Add(entityDefinition, openGenericSystem);
                }
                else if (implementedInterface == typeof(IDeserializeSystem))
                {
                    _genericDeserializeSystemByEntityDefinition.Add(entityDefinition, openGenericSystem);
                }
#if FANTASY_UNITY
                else if (implementedInterface == typeof(ILateUpdateSystem))
                {
                    _genericLateUpdateSystemByEntityDefinition.Add(entityDefinition, openGenericSystem);
                }
#endif
                else
                {
                    Log.Error($"泛型System({openGenericSystem})未实现任何支持的接口，无法注册. Generic-System lack Interface!");
                }
            }
        }

        /// <summary>
        /// 分接口类型注册System
        /// 失败返回 false
        /// </summary>
        private bool RegisterSystemByInterfaceType(object systemInstance, long assemblyIdentity, Type systemType)
        {
            // 根据接口类型添加到对应的字典
            Type entityType = default;
            switch (systemInstance)
            {
                case IAwakeSystem awakeSystem:
                    {
                        entityType = awakeSystem.EntityType();
                        _awakeSystems.Add(entityType, awakeSystem);
                    }
                    break;
                case IDestroySystem destroySystem:
                    {
                        entityType = destroySystem.EntityType();
                        _destroySystems.Add(entityType, destroySystem);
                    }
                    break;
                case IDeserializeSystem deserializeSystem:
                    {
                        entityType = deserializeSystem.EntityType();
                        _deserializeSystems.Add(entityType, deserializeSystem);
                    }
                    break;
                case IUpdateSystem updateSystem:
                    {
                        entityType = updateSystem.EntityType();
                        _updateSystems.Add(entityType, updateSystem);
                    }
                    break;
#if FANTASY_UNITY
                case ILateUpdateSystem lateUpdateSystem:
                    {
                        entityType = lateUpdateSystem.EntityType();
                        _lateUpdateSystems.Add(entityType, lateUpdateSystem);
                    }
                    break;
#endif
                default:
                    Log.Error($"类型({systemType})不受实体系统支持, IEntitySystem not support type ({systemType})");
                    return false;
            }

            // 关联程序集
            _assemblyList.Add(assemblyIdentity, entityType);
            return true;
        }

        /// <summary>
        /// 通过构建的Key来注册自定义System
        /// </summary>
        /// <returns></returns>
        private void RegisterCustomSystemByBuiltKey(object systemInstance, Type customEntitySystemType, long assemblyIdentity)
        {

            var @interface = (ICustomEntitySystem)systemInstance;
            var customEntitySystemKey = new CustomEntitySystemKey(@interface.CustomEventType, @interface.EntityType());
            _customEntitySystems.Add(customEntitySystemKey, @interface);
            _assemblyCustomSystemList.Add(assemblyIdentity, customEntitySystemKey);
        }

        public long GetHashCode(Type type)
        {
            var setting = ProjectSettings.EntitySettings.TypeCodeMode;

            if (setting == TypeCodeMode.FromFullName)
            {
                TryGetTypeHashCodeFromFullName(type, out var code);
                return code;
            }

            return GetRealTimeTypeCode(type);
        }


        /// <summary>
        /// 获取从FullName计算的类型哈希码。
        /// 与 GetRealTimeTypeCode 相比: 未注册的运行时闭包泛型需临时计算，因为Load的时候只能计算开放式泛型的HashCode。
        /// "帧同步"用GetRealTimeTypeCode 会存在问题。
        /// </summary>
        /// <param name="type">要获取预计算哈希码的类型</param>
        /// <param name="hashCode">成功返回的哈希</param>
        /// <returns>返回类型的预计算哈希码</returns>
        private void TryGetTypeHashCodeFromFullName(Type type, out long hashCode)
        {
            if (!_typeHashCodesFromFullName.TryGetValue(type, out hashCode))
            {
                if (type.IsGenericType)
                {
                    hashCode = HashCodeHelper.ComputeHash64(type.FullName);
#if FANTASY_NET
                    Log.Warning($"检测到泛型{type}正在处理HashCode计算, 请务必留意是否需要添加ClosedGenericAttribute以预注册.");
                    Log.Warning($"Generic type {type} detected during HashCode computation. Please ensure a ClosedGenericAttribute is added for pre-registration if necessary.");
#endif
                }
            }
        }


        /// <summary>
        /// 获取实时类型码。
        /// 这个方法相比GetPreloadHashCode理论上速度更快,也无需预计算,代价是"跨域不安全"。
        /// (比如同样一个类型, 热重载前后返回的long值不一致)
        /// 如果域变动, 就涉及到long值迁移, 需要额外实现以确保跨域安全。
        /// "帧同步"需强一致性, 用这个 会存在问题。
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        private long GetRealTimeTypeCode(Type type)
        {
            return type.TypeHandle.Value.ToInt64();
        }

        #endregion

        /// <summary>
        /// 释放实体系统管理器资源
        /// </summary>
        public override void Dispose()
        {
            _updateQueue.Clear();
            _updateQueueDic.Clear();
#if FANTASY_UNITY
            _lateUpdateQueue.Clear();
            _lateUpdateQueueDic.Clear();
            _lateUpdateSystems.Clear();
            _genericLateUpdateSystemByEntityDefinition.Clear();
#endif
            _assemblyList.Clear();

            _awakeSystems.Clear();
            _updateSystems.Clear();
            _destroySystems.Clear();
            _deserializeSystems.Clear();

            _genericAwakeSystemByEntityDefinition.Clear();
            _genericUpdateSystemByEntityDefinition.Clear();
            _genericDestroySystemByEntityDefinition.Clear();
            _genericDeserializeSystemByEntityDefinition.Clear();

            AssemblySystem.UnRegister(this);
            base.Dispose();
        }
    }
}