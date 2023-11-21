using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 地址映射管理组件，用于管理地址映射和锁定。
    /// </summary>
    public sealed class AddressableManageComponent : Entity
    {
        private readonly Dictionary<long, long> _addressable = new();
        private readonly Dictionary<long, WaitCoroutineLock> _locks = new();
        private readonly CoroutineLockQueueType _addressableLock = new CoroutineLockQueueType("AddressableLock");

        /// <summary>
        /// 添加地址映射。
        /// </summary>
        /// <param name="addressableId">地址映射的唯一标识。</param>
        /// <param name="routeId">路由 ID。</param>
        /// <param name="isLock">是否进行锁定。</param>
        public async FTask Add(long addressableId, long routeId, bool isLock)
        {
            WaitCoroutineLock waitCoroutineLock = null;
            try
            {
                if (isLock)
                {
                    waitCoroutineLock = await _addressableLock.Lock(addressableId);
                }
                
                _addressable[addressableId] = routeId;
                Log.Debug($"AddressableManageComponent Add addressableId:{addressableId} routeId:{routeId}");
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                waitCoroutineLock?.Dispose();
            }
        }

        /// <summary>
        /// 获取地址映射的路由 ID。
        /// </summary>
        /// <param name="addressableId">地址映射的唯一标识。</param>
        /// <returns>地址映射的路由 ID。</returns>
        public async FTask<long> Get(long addressableId)
        {
            using (await _addressableLock.Lock(addressableId))
            {
                _addressable.TryGetValue(addressableId, out var routeId);
                return routeId;
            }
        }

        /// <summary>
        /// 移除地址映射。
        /// </summary>
        /// <param name="addressableId">地址映射的唯一标识。</param>
        public async FTask Remove(long addressableId)
        {
            using (await _addressableLock.Lock(addressableId))
            {
                _addressable.Remove(addressableId);
                Log.Debug($"Addressable Remove addressableId: {addressableId} _addressable:{_addressable.Count}");
            }
        }

        /// <summary>
        /// 锁定地址映射。
        /// </summary>
        /// <param name="addressableId">地址映射的唯一标识。</param>
        public async FTask Lock(long addressableId)
        {
            var waitCoroutineLock = await _addressableLock.Lock(addressableId);
            _locks.Add(addressableId, waitCoroutineLock);
        }

        /// <summary>
        /// 解锁地址映射。
        /// </summary>
        /// <param name="addressableId">地址映射的唯一标识。</param>
        /// <param name="routeId">新的路由 ID。</param>
        /// <param name="source">解锁来源。</param>
        public void UnLock(long addressableId, long routeId, string source)
        {
            if (!_locks.Remove(addressableId, out var coroutineLock))
            {
                Log.Error($"Addressable unlock not found addressableId: {addressableId} Source:{source}");
                return;
            }

            _addressable.TryGetValue(addressableId, out var oldAddressableId);

            if (routeId != 0)
            {
                _addressable[addressableId] = routeId;
            }

            coroutineLock.Dispose();
            Log.Debug($"Addressable UnLock key: {addressableId} oldAddressableId : {oldAddressableId} routeId: {routeId}  Source:{source}");
        }
    }
}