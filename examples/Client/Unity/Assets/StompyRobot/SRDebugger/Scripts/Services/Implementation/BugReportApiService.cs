
#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace SRDebugger.Services.Implementation
{
    using System;
    using Internal;
    using SRF;
    using SRF.Service;
    using UnityEngine;

    [Service(typeof (IBugReportService))]
    public class BugReportApiService : SRServiceBase<IBugReportService>, IBugReportService
    {
        public const float Timeout = 12f;
        private BugReportCompleteCallback _completeCallback;
        private string _errorMessage;
        private bool _isBusy;
        private float _previousProgress;
        private BugReportProgressCallback _progressCallback;
        private BugReportApi _reportApi;

        public void SendBugReport(BugReport report, BugReportCompleteCallback completeHandler,
            BugReportProgressCallback progressCallback = null)
        {
            if (report == null)
            {
                throw new ArgumentNullException("report");
            }

            if (completeHandler == null)
            {
                throw new ArgumentNullException("completeHandler");
            }

            if (_isBusy)
            {
                completeHandler(false, "BugReportApiService is already sending a bug report");
                return;
            }

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                completeHandler(false, "No Internet Connection");
                return;
            }

            _errorMessage = "";
            enabled = true;

            _isBusy = true;

            _completeCallback = completeHandler;
            _progressCallback = progressCallback;

            _reportApi = new BugReportApi(report, Settings.Instance.ApiKey);

            StartCoroutine(_reportApi.Submit());
        }

        protected override void Awake()
        {
            base.Awake();

            CachedTransform.SetParent(Hierarchy.Get("SRDebugger"));
        }

        private void OnProgress(float progress)
        {
            if (_progressCallback != null)
            {
                _progressCallback(progress);
            }
        }

        private void OnComplete()
        {
            _isBusy = false;

            enabled = false;

            _completeCallback(_reportApi.WasSuccessful,
                string.IsNullOrEmpty(_reportApi.ErrorMessage) ? _errorMessage : _reportApi.ErrorMessage);

            _completeCallback = null;
        }

        protected override void Update()
        {
            base.Update();

            if (!_isBusy)
            {
                return;
            }

            if (_reportApi == null)
            {
                _isBusy = false;
            }

            if (_reportApi.IsComplete)
            {
                OnComplete();
                return;
            }

            if (_previousProgress != _reportApi.Progress)
            {
                OnProgress(_reportApi.Progress);
                _previousProgress = _reportApi.Progress;
            }
        }
    }
}
