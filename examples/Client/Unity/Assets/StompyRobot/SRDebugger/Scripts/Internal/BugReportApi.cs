namespace SRDebugger.Internal
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using SRF;
    using UnityEngine;
    using System.Text;
    using UnityEngine.Networking;

    class BugReportApi : MonoBehaviour
    {
        private string _apiKey;
        private BugReport _bugReport;
        private bool _isBusy;

        private UnityWebRequest _webRequest;
        private Action<BugReportSubmitResult> _onComplete;
        private IProgress<float> _progress;

        public static void Submit(BugReport report, string apiKey, Action<BugReportSubmitResult> onComplete, IProgress<float> progress)
        {
            var go = new GameObject("BugReportApi");
            go.transform.parent = Hierarchy.Get("SRDebugger");

            var bugReportApi = go.AddComponent<BugReportApi>();
            bugReportApi.Init(report, apiKey, onComplete, progress);
            bugReportApi.StartCoroutine(bugReportApi.Submit());
        }

        private void Init(BugReport report, string apiKey, Action<BugReportSubmitResult> onComplete, IProgress<float> progress)
        {
            _bugReport = report;
            _apiKey = apiKey;
            _onComplete = onComplete;
            _progress = progress;
        }
        
        void Update()
        {
            if (_isBusy && _webRequest != null)
            {
                _progress.Report(_webRequest.uploadProgress);
            }
        }

        IEnumerator Submit()
        {
            if (_isBusy)
            {
                throw new InvalidOperationException("BugReportApi is already sending a bug report");
            }

            // Reset state
            _isBusy = true;
            _webRequest = null;

            string json;
            byte[] jsonBytes;

            try
            {
                json = BuildJsonRequest(_bugReport);
                jsonBytes = Encoding.UTF8.GetBytes(json);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                SetCompletionState(BugReportSubmitResult.Error("Error building bug report."));
                yield break;
            }

            try
            {
                const string jsonContentType = "application/json";

                _webRequest = new UnityWebRequest(SRDebugApi.BugReportEndPoint, UnityWebRequest.kHttpVerbPOST,
                    new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonBytes)
                    {
                        contentType = jsonContentType
                    });
                _webRequest.SetRequestHeader("Accept", jsonContentType);
                _webRequest.SetRequestHeader("X-ApiKey", _apiKey);
            }
            catch (Exception e)
            {
                Debug.LogError(e);

                if (_webRequest != null)
                {
                    _webRequest.Dispose();
                    _webRequest = null;
                }
            }
            
            if (_webRequest == null)
            {
                SetCompletionState(BugReportSubmitResult.Error("Error building bug report request."));
                yield break;
            }

            yield return _webRequest.SendWebRequest();

#if UNITY_2018 || UNITY_2019
            bool isError = _webRequest.isNetworkError;
#else
            bool isError = _webRequest.result == UnityWebRequest.Result.ConnectionError || _webRequest.result == UnityWebRequest.Result.DataProcessingError;
#endif

            if (isError)
            {
                SetCompletionState(BugReportSubmitResult.Error("Request Error: " + _webRequest.error));
                _webRequest.Dispose();
                yield break;
            }

            long responseCode = _webRequest.responseCode;
            var responseJson = _webRequest.downloadHandler.text;

            _webRequest.Dispose();

            if (responseCode != 200)
            {
                SetCompletionState(BugReportSubmitResult.Error("Server: " + SRDebugApiUtil.ParseErrorResponse(responseJson, "Unknown response from server")));
                yield break;
            }

            SetCompletionState(BugReportSubmitResult.Success);
        }

        private void SetCompletionState(BugReportSubmitResult result)
        {
            _bugReport.ScreenshotData = null; // Clear the heaviest data in case something holds onto it
            _isBusy = false;

            if (!result.IsSuccessful)
            {
                Debug.LogError("Bug Reporter Error: " + result.ErrorMessage);
            }

            Destroy(gameObject);
            _onComplete(result);
        }

        private static string BuildJsonRequest(BugReport report)
        {
            var ht = new Hashtable();

            ht.Add("userEmail", report.Email);
            ht.Add("userDescription", report.UserDescription);

            ht.Add("console", CreateConsoleDump());
            ht.Add("systemInformation", report.SystemInformation);
            ht.Add("applicationIdentifier", Application.identifier);

            if (report.ScreenshotData != null)
            {
                ht.Add("screenshot", Convert.ToBase64String(report.ScreenshotData));
            }
            var json = Json.Serialize(ht);

            return json;
        }

        private static List<List<string>> CreateConsoleDump()
        {
            var consoleLog = Service.Console.AllEntries;
            var list = new List<List<string>>(consoleLog.Count);

            foreach (var consoleEntry in consoleLog)
            {
                var entry = new List<string>(5);

                entry.Add(consoleEntry.LogType.ToString());
                entry.Add(consoleEntry.Message);
                entry.Add(consoleEntry.StackTrace);

                if (consoleEntry.Count > 1)
                {
                    entry.Add(consoleEntry.Count.ToString());
                }

                list.Add(entry);
            }

            return list;
        }
    }
}
