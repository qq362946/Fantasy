using Fantasy;

namespace BestGame;

/// 验证绑定session，
/// 返回地图或向地图请求创建unit，
/// 设置网关角色进入地图状态SetRoleEnterMap
/// 确定并将mapScene信息缓存在gateAccount
public class C2G_EnterMapRequestHandler : MessageRPC<C2G_EnterMapRequest,G2C_EnterMapResponse>
{
    protected override async FTask Run(Session session, C2G_EnterMapRequest request, G2C_EnterMapResponse response, Action reply)
    {
        response.ErrorCode = await Check(session, request, response);
    }

    private async FTask<uint> Check(Session session, C2G_EnterMapRequest request, G2C_EnterMapResponse response)
    {
        if (request.RoleId == 0)
            return ErrorCode.H_C2G_EnterGame_ReqHandler_RoleIdIsZero;
        
        var err = LoginHelper.CheckSessionBindAccount(session);

        // 没有正常通过登录的流程
        if (err != ErrorCode.Success)
            return ErrorCode.H_C2G_EnterGame_Error01;

        var sessionPlayer = session.GetComponent<SessionPlayerComponent>();
        var guider = session.Scene.GetComponent<GuiderComponent>();
        var gateAccount = sessionPlayer.gateAccount;
        var accountId = gateAccount.Id;

        // 正在进入游戏中 操作过于频繁
        if (sessionPlayer.EnterState == SessionState.Entering)
            return ErrorCode.Error_EnterGameFast;


        var _LockGateAccountLock = new CoroutineLockQueueType("LockGateAccountLock");
        using (await _LockGateAccountLock.Lock(accountId))
        {
            try
            {
                // 找不到账号信息
                if (gateAccount == null)
                    return ErrorCode.Error_EnterGameGateNotFindAccountInfo;
                
                // 思考，此时gateAccount中是否肯定已有所有角色的缓存
                var gateRole = gateAccount.GetRole(request.RoleId);
                gateAccount.SelectRoleId = request.RoleId;

                // 没有指定角色
                if (gateRole == null)
                    return ErrorCode.H_C2G_EnterGame_NotFoundRole;

                // role有另一个网关seesion信息，可能别处登录，可能掉线延迟下线中
                if (gateAccount.SelectRoleId != 0 && gateRole.sessionRuntimeId != session.RuntimeId)
                {
                    // 他处角色顶下线...
                }

                // 已经进入游戏、延迟下线中
                if (sessionPlayer.EnterState == SessionState.Enter)
                {
                    // accountId = unitId
                    // 以unitId发IAddressableRouteMessage消息
                    MessageHelper.SendAddressable(session.Scene, accountId,
                        new G2M_Return2MapMsg
                        {
                            MapNum = request.MapNum
                        });
                    return ErrorCode.Error_EnterGameAlreadyEnter;
                }

                // 设置进入状态
                sessionPlayer.EnterState = SessionState.Entering;

                // 获取目标地图的mapScene
                // 随机一个目录地图的mapScene，但不需要存库。缓存在gateAccount，维护周期内记住就行
                var mapScene = gateAccount.GetMapScene(request.MapNum,session.Scene.World.Id);

                // 地图传送或创建unit
                if (gateRole.IsInMap())
                {
                    // 地图传送 ...
                }
                else
                {
                    // 向map请求创建unit
                    // 以mapScene路由Id，发IAddressableRouteMessage消息
                    var result = (M2G_CreateUnitResponse)await MessageHelper.CallInnerRoute(session.Scene,mapScene.EntityId,
                        new G2M_CreateUnitRequest()
                        {
                            PlayerId = accountId,
                            SessionRuntimeId = session.RuntimeId,
                            GateSceneRouteId = session.Scene.RuntimeId,
                            RoleInfo = gateRole.ToProto(),
                        });

                    // 缓存AddressableId
                    if(result.ErrorCode != ErrorCode.Success)
                        return result.ErrorCode;
                    
                    gateAccount.AddressableId = result.AddressableId;
                }

                // SetRoleEnterMap
                guider.SetRoleEnterMap(session,request.MapNum);

                if (LoginHelper.CheckSessionValid(session, session.RuntimeId))
                    sessionPlayer.EnterState = SessionState.Enter;
            }
            catch (Exception e)
            {
                Log.Error($"H_C2G_EnterGame_ReqHandler error {e}");
                return ErrorCode.H_C2G_EnterGame_Error01;
            }
            finally
            {
                // Session有效，存在sessionPlayer且未进入游戏，设置状态为None
                if (LoginHelper.CheckSessionValid(session, session.RuntimeId) && 
                    sessionPlayer != null && sessionPlayer.EnterState != SessionState.Enter)
                    sessionPlayer.EnterState = SessionState.None;
                
            }
        }

        return ErrorCode.Success;
    }
}