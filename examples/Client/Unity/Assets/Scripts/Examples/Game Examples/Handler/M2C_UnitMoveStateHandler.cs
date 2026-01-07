using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy
{
    public sealed class M2C_UnitMoveStateHandler : Message<M2C_UnitMoveState>
    {
        protected override async FTask Run(Session session, M2C_UnitMoveState message)
        {
            Log.Debug("2222");
            if (!UnitManageHelper.TryGet(session.Scene,message.UnitId, out var unit))
            {
                Log.Warning($"not found Unit Id:{message.UnitId}");
                return;
            }

            unit.GetComponent<MoveComponent>().SetTargetPosition(message.Pos.UnityPosition);
            await FTask.CompletedTask;
        }
    }
}