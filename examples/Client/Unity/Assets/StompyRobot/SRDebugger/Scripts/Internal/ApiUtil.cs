namespace SRDebugger.Internal
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using SRF;

    public static class SRDebugApiUtil
    {
        public static string ParseErrorException(WebException ex)
        {
            if (ex.Response == null)
            {
                return ex.Message;
            }

            try
            {
                var response = ReadResponseStream(ex.Response);

                return ParseErrorResponse(response);
            }
            catch
            {
                return ex.Message;
            }
        }

        public static string ParseErrorResponse(string response, string fallback = "Unexpected Response")
        {
            try
            {
                var jsonTable = (Dictionary<string, object>) Json.Deserialize(response);

                var error = "";

                error += jsonTable["errorMessage"];

                object value;

                if (jsonTable.TryGetValue("errors", out value) && value is IList<object>)
                {
                    var errors = value as IList<object>;

                    foreach (var e in errors)
                    {
                        error += "\n";
                        error += e;
                    }
                }

                return error;
            }
            catch
            {
                if (response.Contains("<html>"))
                {
                    return fallback;
                }

                return response;
            }
        }

#if UNITY_EDITOR || (!NETFX_CORE && !UNITY_WINRT)

        public static bool ReadResponse(HttpWebRequest request, out string result)
        {
            try
            {
                var response = request.GetResponse();
                result = ReadResponseStream(response);

                return true;
            }
            catch (WebException e)
            {
                result = ParseErrorException(e);
                return false;
            }
        }

#endif

        public static string ReadResponseStream(WebResponse stream)
        {
            using (var responseSteam = stream.GetResponseStream())
            {
                using (var streamReader = new StreamReader(responseSteam))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
