#if FANTASY_NET
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.InnerMessage;
using Fantasy.Network;
// ReSharper disable CheckNamespace
// ReSharper disable ForCanBeConvertedToForeach
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Fantasy.Sphere;

/// <summary>
/// Sphere事件组件，用于管理分布式场景之间的事件订阅和发布
/// </summary>
public sealed class SphereEventComponent : Entity, IAssemblyLifecycle
{
    private bool _isClosed;
    
    #region AssemblyManifest
    
    private readonly HashSet<long> _assemblyManifests = new();
    
    /// <summary>
    /// 初始化SphereEventComponent，将其注册到程序集系统中
    /// </summary>
    /// <returns>返回初始化后的SphereEventComponent实例</returns>
    internal async FTask<SphereEventComponent> Initialize()
    {
        _localSphereEventLock = Scene.CoroutineLockComponent.Create(GetType().TypeHandle.Value.ToInt64());
        _remoteSphereEventLock = Scene.CoroutineLockComponent.Create(GetType().TypeHandle.Value.ToInt64());
        await AssemblyLifecycle.Add(this);
        return this;
    }
    
    /// <summary>
    /// 加载程序集，注册该程序集中的所有事件系统
    /// 支持热重载：如果程序集已加载，会先卸载再重新加载
    /// </summary>
    /// <param name="assemblyManifest">程序集清单对象，包含程序集的元数据和注册器</param>
    /// <returns>异步任务</returns>
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
            assemblyManifest.SphereEventRegistrar.Register(_sphereEvents);
            _assemblyManifests.Add(assemblyManifestId);
            tcs.SetResult();
        });
        await tcs;
    }

    /// <summary>
    /// 卸载程序集，取消注册该程序集中的所有实体系统
    /// </summary>
    /// <param name="assemblyManifest">程序集清单对象，包含程序集的元数据和注册器</param>
    /// <returns>异步任务</returns>
    public async FTask OnUnload(AssemblyManifest assemblyManifest)
    {
        var tcs = FTask.Create(false);
        Scene?.ThreadSynchronizationContext.Post(() =>
        {
            OnUnLoadInner(assemblyManifest);
            tcs.SetResult();
        });
        await tcs;
    }
    
    /// <summary>
    /// 卸载程序集的内部实现
    /// 会清理该程序集注册的所有系统
    /// </summary>
    /// <param name="assemblyManifest">程序集清单对象，包含程序集的元数据和注册器</param>
    private void OnUnLoadInner(AssemblyManifest assemblyManifest)
    {
        assemblyManifest.SphereEventRegistrar.UnRegister(_sphereEvents);
        _assemblyManifests.Remove(assemblyManifest.AssemblyManifestId);
    }

    #endregion
    
    #region Local

    private CoroutineLock _localSphereEventLock;
    
    /// <summary>
    /// 本地订阅的远程服务器信息 (远程RouteId, 事件类型HashCode)
    /// </summary>
    private readonly HashSet<(long, long)> _localSubscribers = new();

    /// <summary>
    /// 本地订阅的事件
    /// </summary>
    private readonly OneToManyHashSet<long, Func<Scene, SphereEventArgs, FTask>> _sphereEvents = new();

    /// <summary>
    /// 订阅远程服务器的Sphere事件
    /// </summary>
    /// <param name="routeId">远程服务器的RouteId</param>
    /// <typeparam name="T">事件类型</typeparam>
    /// <returns></returns>
    public FTask Subscribe<T>(long routeId) where T : SphereEventArgs, new()
    {
        return Subscribe(routeId, TypeHashCache<T>.HashCode);
    }

    /// <summary>
    /// 订阅远程服务器的Sphere事件
    /// </summary>
    /// <param name="routeId"></param>
    /// <param name="typeHashCode">事件类型的HashCode</param>
    public async FTask Subscribe(long routeId, long typeHashCode)
    {
        using (await _localSphereEventLock.Wait(typeHashCode))
        {
            var response = await Scene.NetworkMessagingComponent.CallInnerRoute(routeId, new I_SubscribeSphereEventRequest()
            {
                RouteId = Scene.RouteId,
                TypeHashCode = typeHashCode
            });

            if (response.ErrorCode != 0)
            {
                Log.Error($"SphereEventComponent Subscribe failed with errorCode {response.ErrorCode}");
                return;
            }
        
            _localSubscribers.Add((routeId, typeHashCode));
        }
    }

    /// <summary>
    /// 取消订阅远程服务器的Sphere事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="routeId">远程服务器的RouteId</param>
    public FTask Unsubscribe<T>(long routeId) where T : SphereEventArgs, new()
    {
        return Unsubscribe(routeId, TypeHashCache<T>.HashCode, true);
    }

    /// <summary>
    /// 取消订阅远程服务器的Sphere事件（内部方法）
    /// </summary>
    /// <param name="routeId">远程服务器的RouteId</param>
    /// <param name="typeHashCode">事件类型的HashCode</param>
    /// <param name="sendRemote">是否向远程服务器发送取消订阅请求。
    /// true: 发送 I_UnsubscribeSphereEventRequest 通知远程服务器移除订阅关系（正常取消订阅流程）。
    /// false: 仅在本地移除订阅记录，不通知远程服务器（用于远程服务器已断开或由远程服务器主动撤销的场景）。
    /// </param>
    internal async FTask Unsubscribe(long routeId, long typeHashCode, bool sendRemote)
    {
        using (await _localSphereEventLock.Wait(typeHashCode))
        {
            if (sendRemote)
            {
                var response = await Scene.NetworkMessagingComponent.CallInnerRoute(routeId,
                    new I_UnsubscribeSphereEventRequest()
                    {
                        RouteId = Scene.RouteId,
                        TypeHashCode = typeHashCode
                    });

                if (response.ErrorCode != 0)
                {
                    Log.Error($"SphereEventComponent Unsubscribe failed with errorCode {response.ErrorCode}");
                }
            }

            _localSubscribers.Remove((routeId, typeHashCode));
        }
    }

    /// <summary>
    /// 处理远程发布的事件并调用本地订阅者
    /// </summary>
    /// <param name="routeId">发布者的RouteId</param>
    /// <param name="eventArgs">事件参数</param>
    /// <returns>错误码: Success=成功, ErrHandleRemotePublicationNotSubscribed=未找到订阅关系</returns>
    internal async FTask<uint> HandleRemotePublication(long routeId, SphereEventArgs eventArgs)
    {
        var eventArgsTypeHashCode = eventArgs.TypeHashCode;

        using (await _localSphereEventLock.Wait(eventArgsTypeHashCode))
        {
            // 检查本地是否订阅了该 RouteId 和事件类型的组合
            // 如果未订阅，返回错误码表示没有订阅关系
            if (!_localSubscribers.Contains((routeId, eventArgsTypeHashCode)))
            {
                return InnerErrorCode.ErrHandleRemotePublicationNotSubscribed;
            }

            // 获取该事件类型的所有本地处理器
            // 如果没有注册任何处理器，直接返回成功（这是正常情况，可能还未注册处理器）
            if (!_sphereEvents.TryGetValue(eventArgsTypeHashCode, out var sphereEvents))
            {
                return InnerErrorCode.Success;
            }
        
            // 并发调用所有订阅了该事件的本地处理器
            using var tasks = ListPool<FTask>.Create();

            foreach (var @event in sphereEvents)
            {
                try
                {
                    tasks.Add(@event(Scene, eventArgs));
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }

            // 等待所有事件处理器执行完成
            await FTask.WaitAll(tasks);
            return InnerErrorCode.Success;
        }
    }

    #endregion

    #region Remote
    
    private CoroutineLock _remoteSphereEventLock;

    /// <summary>
    /// 订阅事件的远程服务器信息 (事件类型HashCode -> 远程RouteId列表)
    /// </summary>
    private readonly OneToManyHashSet<long, long> _remoteSubscribers = new();
    
    /// <summary>
    /// 注册远程订阅者
    /// </summary>
    /// <param name="fromRouteId">远程服务器的RouteId</param>
    /// <param name="typeHashCode">事件类型的HashCode</param>
    internal void RegisterRemoteSubscriber(long fromRouteId, long typeHashCode)
    {
        _remoteSubscribers.Add(typeHashCode, fromRouteId);
    }
    
    /// <summary>
    /// 注销远程订阅者
    /// </summary>
    /// <param name="fromRouteId">远程服务器的RouteId</param>
    /// <param name="typeHashCode">事件类型的HashCode</param>
    public async FTask UnregisterRemoteSubscriber(long fromRouteId, long typeHashCode)
    {
        using (await _remoteSphereEventLock.Wait(typeHashCode))
        {
            _remoteSubscribers.RemoveValue(typeHashCode, fromRouteId);
        }
    }

    /// <summary>
    /// 注销远程订阅者
    /// </summary>
    /// <param name="fromRouteId">远程服务器的RouteId</param>
    /// <typeparam name="T">事件类型</typeparam>
    public FTask UnregisterRemoteSubscriber<T>(long fromRouteId) where T : SphereEventArgs, new()
    {
        return UnregisterRemoteSubscriber(fromRouteId, TypeHashCache<T>.HashCode);
    }

    /// <summary>
    /// 撤销远程订阅者的订阅
    /// </summary>
    /// <param name="fromRouteId">远程服务器的RouteId</param>
    /// <typeparam name="T">事件类型</typeparam>
    public FTask RevokeRemoteSubscriber<T>(long fromRouteId) where T : SphereEventArgs, new()
    {
        return RevokeRemoteSubscriber(fromRouteId, TypeHashCache<T>.HashCode);
    }

    /// <summary>
    /// 撤销远程订阅者的订阅
    /// </summary>
    /// <param name="fromRouteId">远程服务器的RouteId</param>
    /// <param name="typeHashCode">事件类型HashCode</param>
    public async FTask RevokeRemoteSubscriber(long fromRouteId, long typeHashCode)
    {
        using (await _remoteSphereEventLock.Wait(typeHashCode))
        {
            var response = await Scene.NetworkMessagingComponent.CallInnerRoute(fromRouteId,
                new I_RevokeRemoteSubscriberRequest()
                {
                    RouteId = Scene.RouteId,
                    TypeHashCode = typeHashCode
                });

            if (response.ErrorCode != 0)
            {
                Log.Error($"SphereEventComponent RevokeRemoteSubscriber failed with errorCode {response.ErrorCode}");
            }
        
            _remoteSubscribers.RemoveValue(typeHashCode, fromRouteId);
        }
    }

    /// <summary>
    /// 向远程订阅者发布事件
    /// </summary>
    /// <param name="sphereEventArgs">事件参数</param>
    /// <param name="isAutoDisposed">是否自动释放事件参数</param>
    public async FTask PublishToRemoteSubscribers(SphereEventArgs sphereEventArgs, bool isAutoDisposed)
    {
        var typeHashCode = sphereEventArgs.TypeHashCode;

        using (await _remoteSphereEventLock.Wait(typeHashCode))
        {
            if (!_remoteSubscribers.TryGetValue(typeHashCode, out var subscribers))
            {
                return;
            }
        
            var routeId = Scene.RouteId;
        
            foreach (var subscriber in subscribers)
            {
                var response = await Scene.NetworkMessagingComponent.CallInnerRoute(subscriber,
                    new I_PublishSphereEventRequest()
                    {
                        RouteId = routeId,
                        SphereEventArgs = sphereEventArgs
                    });
                if (response.ErrorCode != 0)
                {
                    Log.Error($"SphereEventComponent PublishToRemoteSubscribers failed with errorCode {response.ErrorCode}");
                }
            }
        
            if (isAutoDisposed)
            {
                sphereEventArgs.Dispose();
            }
        }
    }

    #endregion

    /// <summary>
    /// 关闭并且清理所有资源，如果有订阅的事件会自动通知其他服务器取消订阅
    /// </summary>
    public async FTask Close()
    {
        if (_isClosed)
        {
            return;
        }
        
        _isClosed = true;
        
        // Local
        var localSubscribersArray = _localSubscribers.ToArray();
        foreach (var (routeId,typeHashCode) in localSubscribersArray)
        {
            await Unsubscribe(routeId, typeHashCode, true);
        }
        _sphereEvents.Clear();
        _remoteSphereEventLock.Dispose();
        _remoteSphereEventLock = null;
        // Remote
        foreach (var (typeHashCode, routeIdList) in _remoteSubscribers)
        {
            foreach (var routeId in routeIdList)
            {
                await RevokeRemoteSubscriber(routeId, typeHashCode);
            }
        }
        _localSphereEventLock.Dispose();
        _localSphereEventLock = null;
    }

    /// <summary>
    /// 销毁方法
    /// </summary>
    public override void Dispose()
    {
        DisposeAsync().Coroutine();
    }

    private async FTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }
        await Close();
        base.Dispose();
    }
}

#endif