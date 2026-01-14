
using System.IO;
using System.Text;
using UnityEngine.Networking;
#pragma warning disable CS0618 // Type or member is obsolete

#if NETFX_CORE
using UnityEngine.Windows;
#endif

namespace SRDebugger.Internal
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Services;
    using SRF;
    using UnityEngine;

    public class BugReportApi
    {
        private readonly string _apiKey;
        private readonly BugReport _bugReport;
        private bool _isBusy;

        private UnityWebRequest _webRequest;

        public BugReportApi(BugReport report, string apiKey)
        {
            _bugReport = report;
            _apiKey = apiKey;
        }

        public bool IsComplete { get; private set; }
        public bool WasSuccessful { get; private set; }
        public string ErrorMessage { get; private set; }

        public float Progress
        {
            get
            {
                if (_webRequest == null)
                {
                    return 0;
                }

                return Mathf.Clamp01(_webRequest.uploadProgress);
            }
        }

        public IEnumerator Submit()
        {
            //Debug.Log("[BugReportApi] Submit()");

            if (_isBusy)
            {
                throw new InvalidOperationException("BugReportApi is already sending a bug report");
            }

            // Reset state
            _isBusy = true;
            ErrorMessage = "";
            IsComplete = false;
            WasSuccessful = false;
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
                ErrorMessage = "Error building bug report.";
                Debug.LogError(e);
                SetCompletionState(false);
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
                ErrorMessage = "Error building bug report request.";
                Debug.LogError(e);

                if (_webRequest != null)
                {
                    _webRequest.Dispose();
                }

                SetCompletionState(false);
            }
            
            if (_webRequest == null)
            {
                SetCompletionState(false);
                yield break;
            }

#if !UNITY_2017_2_OR_NEWER
            yield return _webRequest.Send();
#else
            yield return _webRequest.SendWebRequest();
#endif

            if (_webRequest.isNetworkError)
            {
                ErrorMessage = "Request Error: " + _webRequest.error;
                SetCompletionState(false);
                _webRequest.Dispose();
                yield break;
            }

            long responseCode = _webRequest.responseCode;
            var responseJson = _webRequest.downloadHandler.text;

            _webRequest.Dispose();

            if (responseCode != 200)
            {
                ErrorMessage = "Server: " + SRDebugApiUtil.ParseErrorResponse(responseJson, "Unknown response from server");
                SetCompletionState(false);
                yield break;
            }

            SetCompletionState(true);
        }

        private void SetCompletionState(bool wasSuccessful)
        {
            _bugReport.ScreenshotData = null;
            WasSuccessful = wasSuccessful;
            IsComplete = true;
            _isBusy = false;

            if (!wasSuccessful)
            {
                Debug.LogError("Bug Reporter Error: " + ErrorMessage);
            }
        }

        private static string BuildJsonRequest(BugReport report)
        {
            var ht = new Hashtable();

            ht.Add("userEmail", report.Email);
            ht.Add("userDescription", report.UserDescription);

            ht.Add("console", CreateConsoleDump());
            ht.Add("systemInformation", report.SystemInformation);

            if (report.ScreenshotData != null)
            {
                ht.Add("screenshot", Convert.ToBase64String(report.ScreenshotData));
            }

            var json = Json.Serialize(ht);

            return json;
        }

        private static IList<IList<string>> CreateConsoleDump()
        {
            var list = new List<IList<string>>();

            var consoleLog = Service.Console.AllEntries;

            foreach (var consoleEntry in consoleLog)
            {
                var entry = new List<string>();

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
