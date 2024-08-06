// ReSharper disable SuspiciousTypeConversion.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#if FANTASY_NET
namespace Fantasy;

public sealed class SingleCollectionComponent : Entity, IAssembly
{
    private CoroutineLock _coroutineLock;
    private readonly OneToManyHashSet<Type, string> _collection = new OneToManyHashSet<Type, string>();
    private readonly OneToManyList<long, SingleCollectionInfo> _assemblyCollections = new OneToManyList<long, SingleCollectionInfo>();
    private sealed class SingleCollectionInfo(Type rootType, string collectionName)
    {
        public readonly Type RootType = rootType;
        public readonly string CollectionName = collectionName;
    }

    public async FTask<SingleCollectionComponent> Initialize()
    {
        _coroutineLock = Scene.CoroutineLockComponent.Create(GetType().TypeHandle.Value.ToInt64());
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
                Log.Error($"type {type.FullName} Implemented the interface of ISingleCollection, requiring the implementation of SingleCollectionAttribute");
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

    public async FTask GetCollections<T>(T entity) where T : Entity, ISingleCollectionRoot
    {
        if (!_collection.TryGetValue(typeof(T), out var collections))
        {
            return;
        }
        
        var worldDateBase = Scene.World.DateBase;

        using (await _coroutineLock.Wait(entity.Id))
        {
            foreach (var collectionName in collections)
            {
                var singleCollection = await worldDateBase.QueryNotLock<Entity>(entity.Id, collectionName);
                singleCollection.Deserialize(Scene);
                entity.AddComponent(singleCollection);
            }
        }
    }
    
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
        await entity.Scene.World.DateBase.Save(entity.Id, collections);
    }

    #endregion
}
#endif