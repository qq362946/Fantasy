#if FANTASY_NET
// ReSharper disable SuspiciousTypeConversion.Global

using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Database;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.SeparateTable
{
    /// <summary>
    /// 分表组件，用于管理实体的数据库分表存储功能。
    /// 负责注册、加载和保存标记了 <see cref="SeparateTableAttribute"/> 的实体数据。
    /// </summary>
    /// <remarks>
    /// 此组件实现了程序集生命周期接口，会在程序集加载/卸载时自动注册/反注册分表信息。
    /// 通过 Source Generator 生成的注册器自动管理分表实体的元数据，避免运行时反射。
    /// </remarks>
    public sealed class SeparateTableComponent : Entity, IAssemblyLifecycle
    {
        /// <summary>
        /// 存储已加载的程序集清单ID集合，用于追踪哪些程序集的分表信息已注册。
        /// </summary>
        private readonly HashSet<long> _assemblyManifests = new();

        /// <summary>
        /// 分表信息映射表，键为父实体类型，值为该父实体对应的所有分表信息集合。
        /// 用于快速查询某个实体类型有哪些子实体需要分表存储。
        /// </summary>
        private readonly OneToManyHashSet<long, ISeparateTableRegistrar.SeparateTableInfo> _separateTables = new ();

        #region AssemblyManifest

        /// <summary>
        /// 初始化分表组件，将其注册到程序集生命周期管理器中。
        /// </summary>
        /// <returns>返回初始化后的分表组件实例。</returns>
        internal async FTask<SeparateTableComponent> Initialize()
        {
            await AssemblyLifecycle.Add(this);
            return this;
        }

        /// <summary>
        /// 当程序集加载时的回调方法，负责注册该程序集中的所有分表信息。
        /// </summary>
        /// <param name="assemblyManifest">加载的程序集清单，包含该程序集的所有元数据。</param>
        /// <returns>异步任务。</returns>
        /// <remarks>
        /// 此方法在线程同步上下文中执行，确保线程安全。
        /// 如果程序集已被加载过（如热重载场景），会先卸载旧的注册信息再重新注册。
        /// </remarks>
        public async FTask OnLoad(AssemblyManifest assemblyManifest)
        {
            var tcs = FTask.Create(false);
            var assemblyManifestId = assemblyManifest.AssemblyManifestId;
            Scene?.ThreadSynchronizationContext.Post(() =>
            {
                // 如果程序集已加载，先卸载旧的
                if (_assemblyManifests.Contains(assemblyManifestId))
                {
                    OnUnLoadInner(assemblyManifest);
                }

                // 从 Source Generator 生成的注册器中获取分表信息
                var separateTableInfos = assemblyManifest.SeparateTableRegistrar.Register();

                // 将分表信息按父实体类型进行分组注册
                foreach (var separateTableInfo in separateTableInfos)
                {
                    _separateTables.Add(TypeHashCache.GetHashCode(separateTableInfo.RootType), separateTableInfo);
                }

                _assemblyManifests.Add(assemblyManifestId);
                tcs.SetResult();
            });
            await tcs;
        }

        /// <summary>
        /// 当程序集卸载时的回调方法，负责反注册该程序集中的所有分表信息。
        /// </summary>
        /// <param name="assemblyManifest">卸载的程序集清单。</param>
        /// <returns>异步任务。</returns>
        /// <remarks>
        /// 此方法在线程同步上下文中执行，确保线程安全。
        /// </remarks>
        public FTask OnUnload(AssemblyManifest assemblyManifest)
        {
            var task = FTask.Create(false);
            Scene?.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyManifest);
                task.SetResult();
            });
            return task;
        }

        /// <summary>
        /// 卸载程序集的内部实现，从映射表中移除该程序集的所有分表信息。
        /// </summary>
        /// <param name="assemblyManifest">要卸载的程序集清单。</param>
        private void OnUnLoadInner(AssemblyManifest assemblyManifest)
        {
            // 获取该程序集需要反注册的分表信息
            var separateTableInfos = assemblyManifest.SeparateTableRegistrar.UnRegister();

            // 从映射表中逐个移除
            foreach (var separateTableInfo in separateTableInfos)
            {
                _separateTables.RemoveValue(TypeHashCache.GetHashCode(separateTableInfo.RootType), separateTableInfo);
            }

            _assemblyManifests.Remove(assemblyManifest.AssemblyManifestId);
        }

        #endregion

        #region Collections

        /// <summary>
        /// 从数据库加载指定实体的所有分表数据，并自动建立父子关系。
        /// </summary>
        /// <param name="entity">需要加载分表数据的实体实例。</param>
        /// <param name="database">数据库实例。</param>
        /// <typeparam name="T">实体的泛型类型，必须继承自 <see cref="Entity"/>。</typeparam>
        /// <returns>异步任务。</returns>
        /// <remarks>
        /// 此方法会根据实体类型查找其关联的所有分表配置，逐个从数据库中加载对应的分表实体，
        /// 并通过 AddComponent 方法将这些分表实体作为组件添加到父实体上，建立父子关系。
        /// 如果实体类型没有配置分表信息，则直接返回不做任何操作。
        /// </remarks>
        /// <example>
        /// <code>
        /// var player = await db.Query&lt;Player&gt;(playerId);
        /// await separateTableComponent.Load(player); // 加载玩家的所有分表数据
        /// </code>
        /// </example>
        public async FTask LoadWithSeparateTables<T>(T entity, IDatabase database) where T : Entity
        {
            // 检查该实体类型是否配置了分表
            if (!_separateTables.TryGetValue(entity.TypeHashCode, out var separateTables))
            {
                return;
            }

            // 遍历所有分表配置，逐个加载
            foreach (var separateTable in separateTables)
            {
                // 使用实体ID作为查询条件，加载分表实体
                var separateTableEntity = await database.QueryNotLock<Entity>(
                    entity.Id, true, separateTable.TableName);

                if (separateTableEntity == null)
                {
                    continue;
                }

                // 将加载的分表实体作为组件添加到父实体上
                entity.AddComponent(separateTableEntity);
            }
        }

        /// <summary>
        /// 将实体及其所有分表组件保存到数据库中。
        /// </summary>
        /// <param name="entity">需要保存的实体实例。</param>
        /// <param name="database">数据库实例。</param>
        /// <typeparam name="T">实体的泛型类型，必须继承自 <see cref="Entity"/> 并具有无参构造函数。</typeparam>
        /// <returns>异步任务。</returns>
        /// <remarks>
        /// 此方法会检查实体是否配置了分表信息：
        /// - 如果没有配置分表，则直接保存实体本身到数据库。
        /// - 如果配置了分表，会收集实体上所有需要分表存储的组件，统一批量保存到数据库。
        /// 使用对象池优化列表分配，避免频繁 GC。
        /// </remarks>
        /// <example>
        /// <code>
        /// player.Inventory.Items.Add(newItem);
        /// await separateTableComponent.Save(player); // 保存玩家及分表数据
        /// </code>
        /// </example>
        public async FTask PersistAggregate<T>(T entity, IDatabase database) where T : Entity, new()
        {
            // 检查该实体类型是否配置了分表
            if (!_separateTables.TryGetValue(entity.TypeHashCode, out var separateTables))
            {
                // 没有分表配置，直接保存实体
                await database.Save(entity);
                return;
            }

            // 使用对象池创建列表，避免 GC
            using var saveSeparateTables = ListPool<Entity>.Create(entity);

            // 收集所有需要分表保存的组件
            foreach (var separateTableInfo in separateTables)
            {
                var separateTableEntity = entity.GetComponent(separateTableInfo.EntityType);
                if (separateTableEntity == null)
                {
                    continue;
                }
                saveSeparateTables.Add(separateTableEntity);
            }

            // 批量保存实体及其所有分表组件
            await database.Save(entity.Id, saveSeparateTables);
        }

        #endregion
    }
}

#endif