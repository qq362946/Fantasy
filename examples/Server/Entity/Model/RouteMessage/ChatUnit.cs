namespace Fantasy;

public sealed class ChatUnit : Entity
{
    public long GateRouteId;

    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        GateRouteId = 0;
        base.Dispose();
    }
}