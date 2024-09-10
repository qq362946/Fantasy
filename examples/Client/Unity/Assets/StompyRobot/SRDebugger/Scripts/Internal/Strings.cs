namespace SRDebugger.Internal
{
    public class SRDebugStrings
    {
        public static readonly SRDebugStrings Current = new SRDebugStrings();
        public readonly string Console_MessageTruncated = "-- Message Truncated --";
        public readonly string Console_NoStackTrace = "-- No Stack Trace Available --";
        public readonly string PinEntryPrompt = "Enter code to open debug panel";

        public readonly string Profiler_DisableProfilerInfo =
            "Unity profiler is currently <b>enabled</b>. Disable to improve performance.";

        public readonly string Profiler_EnableProfilerInfo =
            "Unity profiler is currently <b>disabled</b>. Enable to show more information.";

        public readonly string Profiler_NoProInfo =
            "Unity profiler is currently <b>disabled</b>. Unity Pro is required to enable it.";

        public readonly string Profiler_NotSupported = "Unity profiler is <b>not supported</b> in this build.";

        public readonly string ProfilerCameraListenerHelp =
            "This behaviour is attached by the SRDebugger profiler to calculate render times.";
    }
}
