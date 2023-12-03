using System;
using Fantasy;

namespace BestGame
{
    /// 获取角色列表时，在GateAccount缓存Role
    public class C2G_RoleListHandler : MessageRPC<C2G_RoleListRequest, G2C_RoleListResponse>
    {
        protected override async FTask Run(Session session, C2G_RoleListRequest request, G2C_RoleListResponse response,
            Action reply)
        {
            response.ErrorCode = await Check(session, request, response);
        }

        private async FTask<uint> Check(Session session, C2G_RoleListRequest request, G2C_RoleListResponse response)
        {
            var sessionPlayer = session.GetComponent<SessionPlayerComponent>();

            Scene scene = session.Scene;
            long accountId = sessionPlayer.gateAccount.Id;
            var accountManager = scene.GetComponent<GateAccountManager>();

            // 锁定网关帐号,拉取角色列表
            var _LockGateAccountLock = new CoroutineLockQueueType("LockGateAccount");
            using (await _LockGateAccountLock.Lock(accountId))
            {
                // 找不到账号信息
                if (!accountManager.TryGetValue(accountId, out GateAccount gateAccount))
                    return ErrorCode.Error_GetRoleListNotFindAccountInfo;
                
                // 获取角色列表时，缓存Role
                var roles = await gateAccount.GetRoles();
                foreach (var (_, role) in roles) // (_, role) 是元组分解语法
                {
                    response.RoleInfos.Add(role.ToProto());
                }
                // Log.Info($"GateAccount:{accountId} RoleList:{roles.ToJson()}");
            }

            return ErrorCode.Success;
        }
    }
}