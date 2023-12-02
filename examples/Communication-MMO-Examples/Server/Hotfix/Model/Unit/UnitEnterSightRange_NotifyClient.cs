using Fantasy;
namespace BestGame
{
    // 进入视野通知
    public class UnitEnterSightRange_NotifyClient: EventSystem<UnitEnterSightRange>
    {
        public override void Handler(UnitEnterSightRange self)
        {
            AOIEntity a = self.Unit;
            AOIEntity b = self.Enter;
            if (a.Id == b.Id)
            {
                return;
            }

            Unit ua = a.GetParent<Unit>();
            if (ua.UnitType != UnitType.Player) return;
            
            Unit ub = b.GetParent<Unit>();

            UnitHelper.NoticeUnitAdd(ua, ub);
        }
    }
}