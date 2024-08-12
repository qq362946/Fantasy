namespace Fantasy;

public sealed class C2M_TestMessageHandler : Addressable<Unit, C2M_TestMessage>
{
    protected override async FTask Run(Unit unit, C2M_TestMessage message)
    {
        Log.Debug($"C2M_TestMessageHandler = {message.Tag}");
        await FTask.CompletedTask;
    }
}