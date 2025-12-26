using Fantasy.Entitas;

namespace Fantasy;

public sealed class ChatUnit : Entity
{
    public long GateAddress;

    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        GateAddress = 0;
        base.Dispose();
    }
}
