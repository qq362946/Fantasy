using Fantasy;

namespace BestGame;

public class G2M_Return2MapHandler : Addressable<Unit,G2M_Return2MapMsg>
{
    protected override async FTask Run(Unit unit, G2M_Return2MapMsg message)
    {
        // 不再是延迟下线等特殊状态,这样当延迟下线时间到时,unit下线会终止
        unit.unitState = OnlineState.None;
        Log.Info("----->back to map");

        await FTask.CompletedTask;
    }
}