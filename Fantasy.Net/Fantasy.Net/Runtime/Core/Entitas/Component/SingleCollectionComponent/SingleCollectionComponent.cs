// ReSharper disable SuspiciousTypeConversion.Global

using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#if FANTASY_NET
namespace Fantasy.SingleCollection
{
    /// <summary>
    /// 用于处理Entity下的实体进行数据库分表存储的组件
    /// </summary>
    public sealed class SingleCollectionComponent : Entity, IAssembly
    {
        private CoroutineLock _coroutineLock;
        private readonly OneToManyHashSet<Type, string> _collection = new OneToManyHashSet<Type, string>();

        private readonly OneToManyList<long, SingleCollectionInfo> _assemblyCollections =
            new OneToManyList<long, SingleCollectionInfo>();

        private sealed class SingleCollectionInfo(Type rootType, string collectionName)
        {
            public readonly Type RootType = rootType;
            public readonly string CollectionName = collectionName;
        }

        internal async FTask<SingleCollectionComponent> Initialize()
        {
            var coroutineLockType = HashCodeHelper.ComputeHash64(GetType().FullName);
            _coroutineLock = Scene.CoroutineLockComponent.Create(coroutineLockType);
            await AssemblySystem.Register(this);
            return this;
        }

        #region Assembly

        public async FTask Load(long assemblyIdentity)
        {
            var tcs = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                LoadInner(assemblyIdentity);
                tcs.SetResult();
            });
            await tcs;
        }

        public async FTask ReLoad(long assemblyIdentity)
        {
            var tcs = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                LoadInner(assemblyIdentity);
                tcs.SetResult();
            });
            await tcs;
        }

        public async FTask OnUnLoad(long assemblyIdentity)
        {
            var tcs = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                tcs.SetResult();
            });
            await tcs;
        }

        private void LoadInner(long assemblyIdentity)
        {
            foreach (var type in AssemblySystem.ForEach(assemblyIdentity, typeof(ISupportedSingleCollection)))
            {
                var customAttributes = type.GetCustomAttributes(typeof(SingleCollectionAttribute), false);
                if (customAttributes.Length == 0)
                {
                    Log.Error(
                        $"type {type.FullName} Implemented the interface of ISingleCollection, requiring the implementation of SingleCollectionAttribute");
                    continue;
                }

                var singleCollectionAttribute = (SingleCollectionAttribute)customAttributes[0];
                var rootType = singleCollectionAttribute.RootType;
                var collectionName = singleCollectionAttribute.CollectionName;
                _collection.Add(rootType, collectionName);
                _assemblyCollections.Add(assemblyIdentity, new SingleCollectionInfo(rootType, collectionName));
            }
        }

        private void OnUnLoadInner(long assemblyIdentity)
        {
            if (!_assemblyCollections.TryGetValue(assemblyIdentity, out var types))
            {
                return;
            }

            foreach (var singleCollectionInfo in types)
            {
                _collection.RemoveValue(singleCollectionInfo.RootType, singleCollectionInfo.CollectionName);
            }

            _assemblyCollections.RemoveByKey(assemblyIdentity);
        }

        #endregion

        #region Collections

        /// <summary>
        /// 通过数据库获取某一个实体类型下所有的分表数据到当前实体下，并且会自动建立父子关系。
        /// </summary>
        /// <param name="entity">实体实例</param>
        /// <typeparam name="T">实体泛型类型</typeparam>
        public async FTask GetCollections<T>(T entity) where T : Entity, ISingleCollectionRoot
        {
            if (!_collection.TryGetValue(typeof(T), out var collections))
            {
                return;
            }

            var worldDateBase = Scene.World.DataBase;

            using (await _coroutineLock.Wait(entity.Id))
            {
                foreach (var collectionName in collections)
                {
                    var singleCollection = await worldDateBase.QueryNotLock<Entity>(entity.Id, true, collectionName);
                    entity.AddComponent(singleCollection);
                }
            }
        }

        /// <summary>
        /// 存储当前实体下支持分表的组件到数据中，包括存储实体本身。
        /// </summary>
        /// <param name="entity">实体实例</param>
        /// <typeparam name="T">实体泛型类型</typeparam>
        public async FTask SaveCollections<T>(T entity) where T : Entity, ISingleCollectionRoot
        {
            using var collections = ListPool<Entity>.Create();

            foreach (var treeEntity in entity.ForEachSingleCollection)
            {
                if (treeEntity is not ISupportedSingleCollection)
                {
                    continue;
                }

                collections.Add(treeEntity);
            }

            collections.Add(entity);
            await entity.Scene.World.DataBase.Save(entity.Id, collections);
        }

        #endregion
    }
}

#endif