using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy
{
    public sealed class M2C_UnitLeaveHandler : Message<M2C_UnitLeave>
    {
        protected override async FTask Run(Session session, M2C_UnitLeave message)
        {
            UnitManageHelper.Remove(session.Scene, message.UnitId);
            await FTask.CompletedTask;
        }
    }
}