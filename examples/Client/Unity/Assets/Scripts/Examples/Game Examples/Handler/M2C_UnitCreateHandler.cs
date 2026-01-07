using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy
{
    public sealed class M2C_UnitCreateHandler : Message<M2C_UnitCreate>
    {
        protected override async FTask Run(Session session, M2C_UnitCreate message)
        {
            var scene = session.Scene;
            var messageUnit = message.Unit;

            if (UnitManageHelper.Contains(scene, messageUnit.UnitId))
            {
                Log.Warning($"unit {messageUnit.UnitId} is already in use");
                return;
            }
            
            var unit = UnitFactory.CreatePlayer(scene, messageUnit.UnitId, messageUnit.Pos.UnityPosition, message.IsSelf);
            UnitManageHelper.Add(unit);
            await FTask.CompletedTask;
        }
    }
}