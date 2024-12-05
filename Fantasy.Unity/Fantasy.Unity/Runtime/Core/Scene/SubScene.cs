using System.Runtime.Serialization;
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
            NetworkMessagingComponent = rootScene.NetworkMessagingComponent;
    #if FANTASY_NET
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