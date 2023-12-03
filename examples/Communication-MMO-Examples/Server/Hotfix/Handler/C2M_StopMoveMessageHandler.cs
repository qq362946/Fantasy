namespace Fantasy;

public class C2M_StopMoveMessageHandler : Addressable<Unit, C2M_StopMoveMessage>
{
    protected override async FTask Run(Unit unit, C2M_StopMoveMessage message)
    {
        MoveHelper.Stop(unit, NoticeClientType.Aoi);
        await FTask.CompletedTask;
    }
}