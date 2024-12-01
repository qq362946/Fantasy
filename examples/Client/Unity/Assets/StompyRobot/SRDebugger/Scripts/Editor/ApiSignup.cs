#if !DISABLE_SRDEBUGGER

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using SRF;
using UnityEngine;
using SRDebugger.Internal;


namespace SRDebugger.Editor
{
    static class ApiSignup
    {
        public delegate void ApiSignupResultCallback(bool success, string apiKey, string email, string error);

        public static void SignUp(string email, string invoiceNo, ApiSignupResultCallback resultCallback)
        {
            var requestData = new Hashtable();
            requestData["emailAddress"] = email;
            requestData["transactionNumber"] = invoiceNo;

            try
            {
                var request = SendRequest(SRDebugApi.EndPoint + "/user/create", requestData, WebRequestMethods.Http.Post);

                string result;

                var didSucceed = SRDebugApiUtil.ReadResponse(request, out result);

                if (didSucceed)
                {
                    var jsonTable = (Dictionary<string, object>) Json.Deserialize(result);

                    resultCallback(true, (string) jsonTable["apiKey"], (string) jsonTable["emailAddress"], null);
                }
                else
                {
                    resultCallback(false, null, null, SRDebugApiUtil.ParseErrorResponse(result));
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                resultCallback(false, null, null, "Internal Error (" + e.Message + ")");
            }
        }

        public static string Verify(string apiKey)
        {
            var request = SendRequest(SRDebugApi.EndPoint + "/user/test", null, apiKey: apiKey);

            string result;

            SRDebugApiUtil.ReadResponse(request, out result);

            return result;
        }

        private static HttpWebRequest SendRequest(string endpoint, Hashtable data,
            string method = WebRequestMethods.Http.Get, string apiKey = null)
        {
            var request = (HttpWebRequest) WebRequest.Create(endpoint);
            request.Timeout = 15000;
            request.Method = method;

            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.BypassCache);

            request.ContentType = "application/json";
            request.Accept = "application/json";

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("X-ApiKey", apiKey);
            }

            request.KeepAlive = false;

            if (data != null)
            {
                var requestJson = Json.Serialize(data);

                using (var requestStream = request.GetRequestStream())
                {
                    using (var writer = new StreamWriter(requestStream, Encoding.UTF8))
                    {
                        writer.Write(requestJson);
                    }
                }
            }

            return request;
        }
    }
}

#endif