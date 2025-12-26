using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using UnityEngine;

namespace Fantasy
{
    public sealed class M2C_PalyerJoinHandler : Message<M2C_PalyerJoin>
    {
        protected override async FTask Run(Session session, M2C_PalyerJoin message)
        {
            var sceneFlagComponent = session.GetComponent<SceneFlagComponent>();

            if (sceneFlagComponent == null)
            {
                Log.Error("sceneFlagComponent is missing");
                return;
            }

            var positionInfo = message.Unit.Pos;
            Object.InstantiateAsync(
                sceneFlagComponent.PlayerGameObject,
                sceneFlagComponent.Players.transform,
                new Vector3(positionInfo.Pos.X, positionInfo.Pos.Y, positionInfo.Pos.Z),
                Quaternion.identity);
            await FTask.CompletedTask;
        }
    }
}