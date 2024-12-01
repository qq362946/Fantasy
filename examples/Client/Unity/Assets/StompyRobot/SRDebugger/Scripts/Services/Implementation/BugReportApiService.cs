namespace SRDebugger.Services.Implementation
{
    using System;
    using Internal;
    using SRF.Service;
    using UnityEngine;

    [Service(typeof (IBugReportService))]
    class BugReportApiService : IBugReportService
    {
        private IBugReporterHandler _handler = new InternalBugReporterHandler();

        public bool IsUsable
        {
            get
            {
                return _handler != null && _handler.IsUsable;
            }
        }

        public void SetHandler(IBugReporterHandler handler)
        {
            Debug.LogFormat("[SRDebugger] Bug Report handler set to {0}", handler);
            _handler = handler;
        }

        public void SendBugReport(BugReport report, BugReportCompleteCallback completeHandler,
            IProgress<float> progress = null)
        {
            if (_handler == null)
            {
                throw new InvalidOperationException("No bug report handler has been configured.");
            }

            if (!_handler.IsUsable)
            {
                throw new InvalidOperationException("Bug report handler is not usable.");
            }

            if (report == null)
            {
                throw new ArgumentNullException("report");
            }

            if (completeHandler == null)
            {
                throw new ArgumentNullException("completeHandler");
            }
            
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                completeHandler(false, "No Internet Connection");
                return;
            }

            _handler.Submit(report, result => completeHandler(result.IsSuccessful, result.ErrorMessage), progress);
        }
    }
}
