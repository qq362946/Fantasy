using Fantasy;
namespace BestGame
{
    // 离开视野
    public class UnitLeaveSightRange_NotifyClient: EventSystem<UnitLeaveSightRange>
    {
        public override void Handler(UnitLeaveSightRange self)
        {
            AOIEntity a = self.Unit;
            AOIEntity b = self.Leave;

            Unit ua = a.GetParent<Unit>();
            if (ua.UnitType != UnitType.Player) return;
            
            Unit ub = b.GetParent<Unit>();
            
            UnitHelper.NoticeUnitRemove(ua, ub);
        }
    }
}