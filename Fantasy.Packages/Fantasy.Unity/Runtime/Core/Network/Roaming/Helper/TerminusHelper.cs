using System.Runtime.CompilerServices;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Network.Interface;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

#if FANTASY_NET
namespace Fantasy.Network.Roaming;

/// <summary>
/// 提供关联实体到 Terminus 的查询、消息和传送扩展方法。
/// </summary>
public static class TerminusHelper
{
    #region Entity 关联查询

    /// <summary>
    /// 获取实体关联的 Terminus；调用方必须保证实体已经完成关联。
    /// </summary>
    /// <param name="entity">已关联 Terminus 的实体。</param>
    /// <returns>实体当前关联的 Terminus。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Terminus GetLinkTerminus(this Entity entity)
    {
        return entity.GetComponent<TerminusFlagComponent>()!.Terminus;
    }

    /// <summary>
    /// 尝试获取实体关联且仍有效的 Terminus。
    /// </summary>
    /// <param name="entity">要查询的实体。</param>
    /// <param name="terminus">找到的 Terminus。</param>
    /// <returns>存在有效关联时返回 <see langword="true"/>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetLinkTerminus(this Entity entity, out Terminus terminus)
    {
        var terminusFlagComponent = entity.GetComponent<TerminusFlagComponent>();

        if (terminusFlagComponent == null)
        {
            terminus = null;
            return false;
        }

        terminus = terminusFlagComponent.Terminus;
        return terminus != null;
    }

    #endregion

    #region Message 方法

    /// <summary>
    /// 通过实体关联的 Terminus 向源端 Session 转发消息。
    /// </summary>
    /// <param name="entity">已关联 Terminus 的实体。</param>
    /// <param name="message">要转发的漫游消息。</param>
    /// <typeparam name="T">漫游消息类型。</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Send<T>(this Entity entity, T message) where T : IRoamingMessage
    {
        var terminusFlagComponent = entity.GetComponent<TerminusFlagComponent>();

        if (terminusFlagComponent == null)
        {
            Log.Error($"Entity {entity.Id} has no linked Terminus, cannot send message");
            return;
        }

        Terminus terminus = terminusFlagComponent.Terminus;

        if (terminus == null)
        {
            Log.Error($"Entity {entity.Id} TerminusFlagComponent.Terminus is null, cannot send message");
            return;
        }

        terminus.Send(message);
    }

    /// <summary>
    /// 通过实体关联的 Terminus 向另一 roamingType 发送单向消息。
    /// </summary>
    /// <param name="entity">已关联 Terminus 的实体。</param>
    /// <param name="roamingType">目标漫游类型。</param>
    /// <param name="message">要发送的漫游消息。</param>
    /// <typeparam name="T">漫游消息类型。</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Send<T>(this Entity entity, int roamingType, T message) where T : IRoamingMessage
    {
        var terminusFlagComponent = entity.GetComponent<TerminusFlagComponent>();

        if (terminusFlagComponent == null)
        {
            Log.Error($"Entity {entity.Id} has no linked Terminus, cannot send message");
            return;
        }

        Terminus terminus = terminusFlagComponent.Terminus;

        if (terminus == null)
        {
            Log.Error($"Entity {entity.Id} TerminusFlagComponent.Terminus is null, cannot send message");
            return;
        }

        terminus.Send(roamingType, message);
    }

    /// <summary>
    /// 通过实体关联的 Terminus 调用另一 roamingType。
    /// </summary>
    /// <param name="entity">已关联 Terminus 的实体。</param>
    /// <param name="roamingType">目标漫游类型。</param>
    /// <param name="request">要发送的漫游请求。</param>
    /// <typeparam name="T">漫游请求类型。</typeparam>
    /// <returns>目标端响应；实体未关联 Terminus 时返回对应错误响应。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FTask<IResponse> Call<T>(this Entity entity, int roamingType, T request) where T : IRoamingMessage
    {
        var terminusFlagComponent = entity.GetComponent<TerminusFlagComponent>();

        if (terminusFlagComponent == null)
        {
            Log.Error($"Entity {entity.Id} has no linked Terminus, cannot call message");
            return FTask<IResponse>.FromResult(entity.Scene.MessageDispatcherComponent.CreateResponse(request.OpCode(), InnerErrorCode.ErrTerminusNotLinked));
        }

        Terminus terminus = terminusFlagComponent.Terminus;

        if (terminus == null)
        {
            Log.Error($"Entity {entity.Id} TerminusFlagComponent.Terminus is null, cannot call message");
            return FTask<IResponse>.FromResult(entity.Scene.MessageDispatcherComponent.CreateResponse(request.OpCode(), InnerErrorCode.ErrTerminusNotLinked));
        }

        return terminus.Call(roamingType, request);
    }

    #endregion

    #region Transfer 方法

    /// <summary>
    /// 将实体关联的 Terminus 和实体本身传送到目标 Scene。
    /// </summary>
    /// <param name="entity">已关联 Terminus 的实体。</param>
    /// <param name="targetSceneAddress">目标 Scene 地址。</param>
    /// <returns>0 表示成功；实体未关联或传送失败时返回对应错误码。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FTask<uint> StartTransfer(this Entity entity, long targetSceneAddress)
    {
        var terminusFlagComponent = entity.GetComponent<TerminusFlagComponent>();

        if (terminusFlagComponent == null)
        {
            Log.Error($"Entity {entity.Id} has no linked Terminus, cannot start transfer");
            return FTask<uint>.FromResult(InnerErrorCode.ErrTerminusNotLinked);
        }

        Terminus terminus = terminusFlagComponent.Terminus;

        if (terminus == null)
        {
            Log.Error($"Entity {entity.Id} TerminusFlagComponent.Terminus is null, cannot start transfer");
            return FTask<uint>.FromResult(InnerErrorCode.ErrTerminusNotLinked);
        }

        return terminus.StartTransfer(targetSceneAddress);
    }

    #endregion
}
#endif
