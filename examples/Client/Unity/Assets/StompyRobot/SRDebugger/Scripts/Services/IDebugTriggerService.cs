namespace SRDebugger.Services
{
    public interface IDebugTriggerService
    {
        bool IsEnabled { get; set; }
        PinAlignment Position { get; set; }
    }
}
