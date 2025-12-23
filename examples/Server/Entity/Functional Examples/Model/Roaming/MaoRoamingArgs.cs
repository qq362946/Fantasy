using Fantasy.Entitas;
using MemoryPack;

namespace Fantasy.Model.Roaming;
[MemoryPackable]
public sealed partial class MaoRoamingArgs : Entity
{
    public string Tag;

    public override void Dispose()
    {
        Tag = string.Empty;
        base.Dispose();
    }
}