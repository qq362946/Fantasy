#if FANTASY_NET
namespace Fantasy
{
    /// <summary>
    /// 可寻址消息组件、挂载了这个组件可以接收Addressable消息
    /// </summary>
    public sealed class AddressableMessageComponent : Entity
    {
        /// <summary>
        /// 可寻址消息组件的唯一标识。
        /// </summary>
        public long AddressableId;

        /// <summary>
        /// 释放资源并解除地址映射。
        /// </summary>
        public override void Dispose()
        {
            if (AddressableId != 0)
            {
                AddressableHelper.RemoveAddressable(Scene.Server, AddressableId).Coroutine();
                AddressableId = 0;
            }
            
            base.Dispose();
        }

        /// <summary>
        /// 注册可寻址消息组件。
        /// </summary>
        /// <param name="isLock">是否进行锁定。</param>
        public FTask Register(bool isLock = true)
        {
            if (Parent == null)
            {
                throw new Exception("AddressableRouteComponent must be mounted under a component");
            }

            AddressableId = Parent.Id;
            
            if (AddressableId == 0)
            {
                throw new Exception("AddressableRouteComponent.Parent.Id is null");
            }

#if FANTASY_DEVELOP
            Log.Debug($"AddressableMessageComponent Register addressableId:{AddressableId} RouteId:{Parent.RuntimeId}");
#endif
            return AddressableHelper.AddAddressable(Scene, AddressableId, Parent.RuntimeId, isLock);
        }

        /// <summary>
        /// 锁定可寻址消息组件。
        /// </summary>
        public FTask Lock()
        {
#if FANTASY_DEVELOP
            Log.Debug($"AddressableMessageComponent Lock {Parent.Id}");
#endif
            return AddressableHelper.LockAddressable(Scene, Parent.Id);
        }

        /// <summary>
        /// 解锁可寻址消息组件。
        /// </summary>
        /// <param name="source">解锁来源。</param>
        public FTask UnLock(string source)
        {
#if FANTASY_DEVELOP
            Log.Debug($"AddressableMessageComponent UnLock {Parent.Id} {Parent.RuntimeId}");
#endif
            return AddressableHelper.UnLockAddressable(Scene, Parent.Id, Parent.RuntimeId, source);
        }
    }
}
#endif