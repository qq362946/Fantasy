using System;

namespace SRDebugger.Services
{
    public delegate void BugReportCompleteCallback(bool didSucceed, string errorMessage);

    public delegate void BugReportProgressCallback(float progress);

    public interface IBugReportService
    {
        /// <summary>
        /// Whether the bug reporter is available for use right now.
        /// </summary>
        bool IsUsable { get; }

        /// <summary>
        /// Set the handler that will submit bug reports.
        /// </summary>
        void SetHandler(IBugReporterHandler handler);

        /// <summary>
        /// Submit a bug report.
        /// completeHandler can be invoked any time after the method is called
        /// (even before the method has returned in case of no internet).
        /// </summary>
        /// <param name="report">Bug report to send</param>
        /// <param name="completeHandler">Delegate to call once bug report is submitted successfully</param>
        /// <param name="progressCallback">Optionally provide a callback for when progress % is known</param>
        void SendBugReport(BugReport report, BugReportCompleteCallback completeHandler,
            IProgress<float> progressCallback = null);
    }
}
