using Fantasy.Event;

namespace Fantasy
{
    public sealed class OnUnitMoveStateChange_TargetMarker : EventSystem<OnUnitMoveStateChange>
    {
        protected override void Handler(OnUnitMoveStateChange self)
        {
            switch (self.State)
            {
                case UnitMoveState.StartMove:
                {
                    var targetMarkerComponent = self.Unit.GetComponent<TargetMarkerComponent>();
            
                    if (targetMarkerComponent == null)
                    {
                        return;
                    }
                    
                    targetMarkerComponent.UpdateMarkerPosition(ref self.Position);
                    return;
                }
                case UnitMoveState.TargetPoint:
                {
                    var targetMarkerComponent = self.Unit.GetComponent<TargetMarkerComponent>();
            
                    if (targetMarkerComponent == null)
                    {
                        return;
                    }
                    
                    targetMarkerComponent.HideMarker();
                    return;
                }
            }
        }
    }
}