using UnityEngine;
using Fantasy;

namespace BestGame
{
	public class M2C_CreateUnitsHandler : Message<M2C_NoticeUnitAdd>
	{
		protected override async FTask Run(Session session, M2C_NoticeUnitAdd message)
		{
			foreach (var unit in message.RoleInfos)
			{
				var temp = new GameObject();
				temp.transform.position = MessageInfoHelper.Vector3(unit.LastMoveInfo);
				temp.transform.rotation = MessageInfoHelper.Quaternion(unit.LastMoveInfo);
				
				GameManager.Ins.AddUnit2Scene(unit.Class,unit.RoleId.ToJson(),temp.transform);
				GameObject.Destroy(temp);
				
			}
			await FTask.CompletedTask;
		}
	}

	public class M2C_RemvoeUnitsHandler : Message<M2C_NoticeUnitRemove>
	{
		protected override async FTask Run(Session session, M2C_NoticeUnitRemove message)
		{
			
			await FTask.CompletedTask;
		}
	}

}
