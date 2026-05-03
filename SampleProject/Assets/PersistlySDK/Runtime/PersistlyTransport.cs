#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Persistly.Unity
{
    public sealed class PersistlyTransportRequest
    {
        public PersistlyTransportRequest(string method, string url, string? body, int timeoutSeconds, IReadOnlyDictionary<string, string>? headers = null)
        {
            Method = method;
            Url = url;
            Body = body;
            TimeoutSeconds = timeoutSeconds;
            Headers = headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Method { get; }

        public string Url { get; }

        public string? Body { get; }

        public int TimeoutSeconds { get; }

        public IReadOnlyDictionary<string, string> Headers { get; }
    }

    public sealed class PersistlyTransportResponse
    {
        public PersistlyTransportResponse(int statusCode, string body, string? error = null)
        {
            StatusCode = statusCode;
            Body = body;
            Error = error;
        }

        public int StatusCode { get; }

        public string Body { get; }

        public string? Error { get; }

        public bool HasBody => !string.IsNullOrEmpty(Body);
    }

    public interface IPersistlyTransport
    {
        Task<PersistlyTransportResponse> SendAsync(PersistlyTransportRequest request, CancellationToken cancellationToken);
    }

    public sealed class UnityWebRequestTransport : IPersistlyTransport
    {
        public async Task<PersistlyTransportResponse> SendAsync(PersistlyTransportRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using (var webRequest = new UnityWebRequest(request.Url, request.Method))
            {
                webRequest.timeout = request.TimeoutSeconds;
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                if (!string.IsNullOrEmpty(request.Body))
                {
                    var requestBytes = Encoding.UTF8.GetBytes(request.Body);
                    webRequest.uploadHandler = new UploadHandlerRaw(requestBytes);
                    webRequest.uploadHandler.contentType = "application/json";
                }

                foreach (var header in request.Headers)
                {
                    webRequest.SetRequestHeader(header.Key, header.Value);
                }

                using (cancellationToken.Register(webRequest.Abort))
                {
                    var operation = webRequest.SendWebRequest();
                    while (!operation.isDone)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }
                }

                return new PersistlyTransportResponse(
                    (int)webRequest.responseCode,
                    webRequest.downloadHandler != null ? webRequest.downloadHandler.text : string.Empty,
                    string.IsNullOrEmpty(webRequest.error) ? null : webRequest.error);
            }
        }
    }
}
