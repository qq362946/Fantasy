namespace Fantasy;

public sealed class AccountManageComponent : Entity
{
    public readonly Dictionary<long, Unit> Units = new Dictionary<long, Unit>();
}