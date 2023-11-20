using Fantasy.Core.Network;
using Fantasy;

namespace BestGame;

public class G2M_Return2MapHandler : Addressable<Unit,G2M_Return2MapMsg>
{
    protected override async FTask Run(Unit unit, G2M_Return2MapMsg message)
    {
        // 不再是延迟下线等特殊状态
        unit.unitState = UnitState.None;

        Log.Info("----->back to map");

        await FTask.CompletedTask;
    }
}