using UnityEngine;
using Fantasy;

namespace BestGame
{
	public class M2C_UnitMoveHandler : Message<M2C_MoveBroadcast>
	{
		protected override async FTask Run(Session session, M2C_MoveBroadcast message)
		{
			Sender.Ins.Recive(ReciveType.UnitMove,message);
			// Log.Info(message.Moves.ToJson());
			
			await FTask.CompletedTask;
		}
	}

	public class M2C_CreateUnitsHandler : Message<M2C_UnitCreate>
	{
		protected override async FTask Run(Session session, M2C_UnitCreate message)
		{
			Sender.Ins.Recive(ReciveType.CreateUnits,message);
			Log.Info(message.UnitInfos.ToJson());
			
			await FTask.CompletedTask;
		}
	}

	public class M2C_RemvoeUnitsHandler : Message<M2C_UnitRemove>
	{
		protected override async FTask Run(Session session, M2C_UnitRemove message)
		{
			
			await FTask.CompletedTask;
		}
	}

}
