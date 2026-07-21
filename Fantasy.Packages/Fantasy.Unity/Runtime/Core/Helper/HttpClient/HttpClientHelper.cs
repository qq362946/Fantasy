#if !FANTASY_WEBGL
#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Pool;

namespace Fantasy.Http
{
    /// <summary>
    /// HTTP帮助类
    /// </summary>
    public static partial class HttpClientHelper
    {
        private static readonly HttpClient Client = new();

        #region String Response

        public static FTask<string> CallNotDeserializeByPost(string url, HttpContent content)
        {
            return CallNotDeserializeByPost(url, content, null);
        }

        public static async FTask<string> CallNotDeserializeByPost(string url, HttpContent content, IReadOnlyDictionary<string, string>? headers)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;

            try
            {
                AddHeaders(request, headers);
                using var response = await Client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            finally
            {
                // HttpContent由调用方管理，避免Dispose request时重复释放。
                request.Content = null;
            }
        }

        public static FTask<string> CallNotDeserializeByGet(string url)
        {
            return CallNotDeserializeByGet(url, null);
        }

        public static async FTask<string> CallNotDeserializeByGet(string url, IReadOnlyDictionary<string, string>? headers)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            AddHeaders(request, headers);

            using var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        #endregion

        #region JSON Response

        public static FTask<T> CallByPost<T>(string url, HttpContent content)
        {
            return CallByPost<T>(url, content, null);
        }

        public static async FTask<T> CallByPost<T>(string url, HttpContent content, IReadOnlyDictionary<string, string>? headers)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;

            try
            {
                AddHeaders(request, headers);
                return await Deserialize<T>(await Client.SendAsync(request));
            }
            finally
            {
                // HttpContent仍归调用方所有。
                request.Content = null;
            }
        }

        public static FTask<T> CallByPost<T>(string url, HttpMethod method)
        {
            return CallByPost<T>(url, method, null);
        }

        public static async FTask<T> CallByPost<T>(string url, HttpMethod method, IReadOnlyDictionary<string, string>? headers)
        {
            using var request = new HttpRequestMessage(method, url);

            AddHeaders(request, headers);

            return await Deserialize<T>(await Client.SendAsync(request));
        }

        public static FTask<T> CallByGet<T>(string url)
        {
            return CallByGet<T>(url, null);
        }

        public static async FTask<T> CallByGet<T>(string url, IReadOnlyDictionary<string, string>? headers)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            AddHeaders(request, headers);

            return await Deserialize<T>(await Client.SendAsync(request));
        }

        #endregion

        #region JSON RPC

        public static FTask<TResponse> Call<TRequest, TResponse>(
            string url,
            int id,
            AuthenticationHeaderValue authentication,
            string method,
            params object[] @params)
            where TRequest : class, IJsonRpcRequest, new()
        {
            return Call<TRequest, TResponse>(
                url,
                id,
                authentication,
                null,
                method,
                @params);
        }

        public static async FTask<TResponse> Call<TRequest, TResponse>(
            string url,
            int id,
            AuthenticationHeaderValue authentication,
            IReadOnlyDictionary<string, string>? headers,
            string method,
            params object[] @params)
            where TRequest : class, IJsonRpcRequest, new()
        {
            var rpcRequest = Pool<TRequest>.Rent();

            try
            {
                rpcRequest.Init(method, id, @params);

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(rpcRequest.ToJson(), Encoding.UTF8, "application/json");
                AddHeaders(request, headers);

                // 强类型认证参数优先于Header字典中的Authorization。
                request.Headers.Authorization = authentication;

                return await Deserialize<TResponse>(await Client.SendAsync(request));
            }
            finally
            {
                Pool<TRequest>.Return(rpcRequest);
            }
        }

        #endregion

        #region Helper

        private static void AddHeaders(HttpRequestMessage request, IReadOnlyDictionary<string, string>? headers)
        {
            if (headers == null)
            {
                return;
            }

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                {
                    // ReSharper disable once NotResolvedInText
                    throw new ArgumentException("Header key 不能为空或空白字符。", "header.Key");
                }

                if (header.Value == null)
                {
                    // ReSharper disable once NotResolvedInText
                    throw new ArgumentNullException("header.Value");
                }
                
                request.Headers.Add(header.Key, header.Value);
            }
        }

        private static async FTask<T> Deserialize<T>(HttpResponseMessage response)
        {
            using (response)
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                return json.Deserialize<T>();
            }
        }

        #endregion
    }
}
#endif