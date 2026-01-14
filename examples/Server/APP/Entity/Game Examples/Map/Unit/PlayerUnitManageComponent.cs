using Fantasy.Entitas;

namespace Fantasy;

public sealed class PlayerUnitManageComponent : Entity
{
    public readonly Dictionary<long, PlayerUnit> Units = new Dictionary<long, PlayerUnit>();
}