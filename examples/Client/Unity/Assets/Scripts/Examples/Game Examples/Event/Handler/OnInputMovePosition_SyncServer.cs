using Fantasy.Async;
using Fantasy.Event;

namespace Fantasy
{
    public sealed class OnInputMovePosition_SyncServer : EventSystem<OnInputMovePosition>
    {
        protected override void Handler(OnInputMovePosition self)
        {
            SendToServer(self).Coroutine();
        }

        private async FTask SendToServer(OnInputMovePosition self)
        {
            using var position = Position.Create();
            position.Transform(self.Position);
            var response = await self.Unit.Scene.Session.C2M_MoveRequest(position);
            if (response.ErrorCode != 0)
            {
                Log.Error($"无法移动到目标 ErrorCode:{response.ErrorCode}");
                return;
            }
        }
    }
}