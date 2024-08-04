namespace Fantasy;

public sealed class GateComponentAwakeSystem : AwakeSystem<GateComponent>
{
    protected override void Awake(GateComponent self)
    {
        Log.Debug($"GateComponent:{Thread.CurrentThread.ManagedThreadId}");
    }
}