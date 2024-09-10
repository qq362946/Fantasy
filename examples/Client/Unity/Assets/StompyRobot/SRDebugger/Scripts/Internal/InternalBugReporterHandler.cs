using System;
using SRF;
using SRF.Service;

namespace SRDebugger.Internal
{
    /// <summary>
    /// The default bug report handler - this submits to the SRDebugger API using the API key configured in the SRDebugger
    /// settings window.
    /// </summary>
    internal class InternalBugReporterHandler : IBugReporterHandler
    {
        public bool IsUsable
        {
            get { return Settings.Instance.EnableBugReporter && !string.IsNullOrWhiteSpace(Settings.Instance.ApiKey); }
        }

        public void Submit(BugReport report, Action<BugReportSubmitResult> onComplete, IProgress<float> progress)
        {
            BugReportApi.Submit(report, Settings.Instance.ApiKey, onComplete, progress);
        }
    }
}