using System.Runtime.Serialization;
using Fantasy.Entitas;
using Newtonsoft.Json;
using Fantasy.Network;
using MongoDB.Bson.Serialization.Attributes;
using ProtoBuf;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Fantasy
{
    /// <summary>
    /// 代表一个Scene下的子Scene
    /// </summary>
    public sealed partial class SubScene : Scene
    {
        /// <summary>
        /// 当前子Scene的父Scene
        /// </summary>
        [BsonIgnore] 
        [JsonIgnore] 
        [ProtoIgnore]
        [IgnoreDataMember]
        public Scene RootScene { get; internal set; }
        
        internal void Initialize(Scene rootScene) 
        {
            EntityPool = rootScene.EntityPool;
            EntityListPool = rootScene.EntityListPool;
            EntitySortedDictionaryPool = rootScene.EntitySortedDictionaryPool;
            SceneUpdate = rootScene.SceneUpdate;
            TimerComponent = rootScene.TimerComponent;
            EventComponent = rootScene.EventComponent;
            EntityComponent = rootScene.EntityComponent;
            MessagePoolComponent = rootScene.MessagePoolComponent;
            CoroutineLockComponent = rootScene.CoroutineLockComponent;
            MessageDispatcherComponent = rootScene.MessageDispatcherComponent;
    #if FANTASY_NET
            NetworkMessagingComponent = rootScene.NetworkMessagingComponent;
            SingleCollectionComponent = rootScene.SingleCollectionComponent;
    #endif
            ThreadSynchronizationContext = rootScene.ThreadSynchronizationContext;
        }
    
        /// <summary>
        /// 子Scene的销毁方法
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            RootScene.RemoveEntity(RuntimeId);
            RootScene = null;
            base.Dispose();
        }

        /// <summary>
        /// 添加一个实体到当前Scene下
        /// </summary>
        /// <param name="entity">实体实例</param>
        public override void AddEntity(Entity entity)
        {
            RootScene.AddEntity(entity);
        }
        
        /// <summary>
        /// 根据RunTimeId查询一个实体
        /// </summary>
        /// <param name="runTimeId">实体的RunTimeId</param>
        /// <returns>返回的实体</returns>
        public override Entity GetEntity(long runTimeId)
        {
            return RootScene.GetEntity(runTimeId);
        }

        /// <summary>
        /// 根据RunTimeId查询一个实体
        /// </summary>
        /// <param name="runTimeId">实体的RunTimeId</param>
        /// <param name="entity">实体实例</param>
        /// <returns>返回一个bool值来提示是否查找到这个实体</returns>
        public override bool TryGetEntity(long runTimeId, out Entity entity)
        {
            return RootScene.TryGetEntity(runTimeId, out entity);
        }
        
        /// <summary>
        /// 根据RunTimeId查询一个实体
        /// </summary>
        /// <param name="runTimeId">实体的RunTimeId</param>
        /// <typeparam name="T">要查询实体的泛型类型</typeparam>
        /// <returns>返回的实体</returns>
        public override T GetEntity<T>(long runTimeId)
        {
            return RootScene.GetEntity<T>(runTimeId);
        }

        /// <summary>
        /// 根据RunTimeId查询一个实体
        /// </summary>
        /// <param name="runTimeId">实体的RunTimeId</param>
        /// <param name="entity">实体实例</param>
        /// <typeparam name="T">要查询实体的泛型类型</typeparam>
        /// <returns>返回一个bool值来提示是否查找到这个实体</returns>
        public override bool TryGetEntity<T>(long runTimeId, out T entity)
        {
            return RootScene.TryGetEntity(runTimeId, out entity);
        }

        /// <summary>
        /// 删除一个实体，仅是删除不会指定实体的销毁方法
        /// </summary>
        /// <param name="runTimeId">实体的RunTimeId</param>
        /// <returns>返回一个bool值来提示是否删除了这个实体</returns>
        public override bool RemoveEntity(long runTimeId)
        {
            return RootScene.RemoveEntity(runTimeId);
        }

        /// <summary>
        /// 删除一个实体，仅是删除不会指定实体的销毁方法
        /// </summary>
        /// <param name="entity">实体实例</param>
        /// <returns>返回一个bool值来提示是否删除了这个实体</returns>
        public override bool RemoveEntity(Entity entity)
        {
            return RootScene.RemoveEntity(entity);
        }

#if FANTASY_NET
        /// <summary>
        /// 根据runTimeId获得Session
        /// </summary>
        /// <param name="runTimeId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public override Session GetSession(long runTimeId)
        {
            return RootScene.GetSession(runTimeId);
        }
    #endif
    }
}