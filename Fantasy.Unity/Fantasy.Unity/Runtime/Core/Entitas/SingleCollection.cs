// #if FANTASY_NET
//
// // ReSharper disable SuspiciousTypeConversion.Global
//
// namespace Fantasy;
//
// /// <summary>
// /// 单例集合管理器类，继承自 <see cref="Singleton{T}"/>。
// /// </summary>
// public class SingleCollection : Singleton<SingleCollection>
// {
//     private readonly OneToManyHashSet<Type, string> _collection = new OneToManyHashSet<Type, string>();
//     private readonly OneToManyList<int, SingleCollectionInfo> _assemblyCollections = new OneToManyList<int, SingleCollectionInfo>();
//
//     /// <summary>
//     /// 表示单例集合的信息类。
//     /// </summary>
//     private sealed class SingleCollectionInfo(Type rootType, string collectionName)
//     {
//         public readonly Type RootType = rootType;
//         public readonly string CollectionName = collectionName;
//     }
//
//     /// <summary>
//     /// 在程序集加载时执行的方法。
//     /// </summary>
//     /// <param name="assemblyName">程序集名称。</param>
//     protected override void OnLoad(int assemblyName)
//     {
//         foreach (var type in AssemblySystem.ForEach(assemblyName, typeof(ISupportedSingleCollection)))
//         {
//             var customAttributes = type.GetCustomAttributes(typeof(SingleCollectionAttribute), false);
//
//             if (customAttributes.Length == 0)
//             {
//                 Log.Error($"type {type.FullName} Implemented the interface of ISingleCollection, requiring the implementation of SingleCollectionAttribute");
//                 continue;
//             }
//
//             var singleCollectionAttribute = (SingleCollectionAttribute)customAttributes[0];
//             var rootType = singleCollectionAttribute.RootType;
//             var collectionName = singleCollectionAttribute.CollectionName;
//             _collection.Add(rootType, collectionName);
//             _assemblyCollections.Add(assemblyName, new SingleCollectionInfo(rootType, collectionName));
//         }
//     }
//
//     /// <summary>
//     /// 在程序集卸载时执行的方法。
//     /// </summary>
//     /// <param name="assemblyName">程序集名称。</param>
//     protected override void OnUnLoad(int assemblyName)
//     {
//         if (!_assemblyCollections.TryGetValue(assemblyName, out var types))
//         {
//             return;
//         }
//         
//         foreach (var singleCollectionInfo in types)
//         {
//             _collection.RemoveValue(singleCollectionInfo.RootType, singleCollectionInfo.CollectionName);
//         }
//             
//         _assemblyCollections.RemoveByKey(assemblyName);
//     }
//
//     /// <summary>
//     /// 异步获取实体的集合数据。
//     /// </summary>
//     /// <param name="entity">实体对象。</param>
//     /// <returns>表示异步操作的任务。</returns>
//     public async FTask GetCollections(Entity entity)
//     {
//         if (entity is not ISingleCollectionRoot)
//         {
//             return;
//         }
//         
//         if (!_collection.TryGetValue(entity.GetType(), out var collections))
//         {
//             return;
//         }
//
//         var scene = entity.Scene;
//         var worldDateBase = scene.World.DateBase;
//
//         using (await IDateBase.DataBaseLock.Lock(entity.Id))
//         {
//             foreach (var collectionName in collections)
//             {
//                 var singleCollection = await worldDateBase.QueryNotLock<Entity>(entity.Id, collectionName);
//                 singleCollection.Deserialize(scene);
//                 entity.AddComponent(singleCollection);
//             }
//         }
//     }
//
//     /// <summary>
//     /// 异步保存实体的集合数据。
//     /// </summary>
//     /// <param name="entity">实体对象。</param>
//     /// <returns>表示异步操作的任务。</returns>
//     public async FTask SaveCollections(Entity entity)
//     {
//         if (entity is not ISingleCollectionRoot)
//         {
//             return;
//         }
//
//         using var collections = ListPool<Entity>.Create();
//         
//         foreach (var treeEntity in entity.ForEachSingleCollection)
//         {
//             if (treeEntity is not ISupportedSingleCollection)
//             {
//                 continue;
//             }
//             
//             collections.Add(treeEntity);
//         }
//         
//         collections.Add(entity);
//         await entity.Scene.World.DateBase.Save(entity.Id, collections);
//     }
// }
// #endif