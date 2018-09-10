// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Shared;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.SignalR.Samples.Serverless
{
    public class ServerHandler : IDisposable
    {
        private HttpClient _client;
        private readonly string _serverName;

        private readonly ServiceUtils _serviceUtils;

        private readonly string[] _hubNames;

        private readonly string _endpoint;

        private string _target = ServiceUtils.MethodName;

        private bool _disposed = false;

        public ServerHandler(string connectionString, string[] hubNames)
        {
            _client = new HttpClient();
            _serverName = GenerateServerName();
            _serviceUtils = new ServiceUtils(connectionString);
            _hubNames = hubNames;
            _endpoint = _serviceUtils.Endpoint;
        }

        public async Task SendToUsers(string hubName, int maxUser)
        {
            var random = new Random();
            for (var i = 0; i < maxUser; i++)
            {
                var url = GetSendToUserUrl(hubName, $"{ServiceUtils.ClientUserIdPrefix}{i}");
                var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url));

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _serviceUtils.GenerateAccessToken(url, _serverName));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var payloadRequest = new PayloadMessage
                {
                    Target = _target,
                    Arguments = new[]
                    {
                    _serverName,
                    $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                }
                };
                request.Content = new StringContent(JsonConvert.SerializeObject(payloadRequest), Encoding.UTF8, "application/json");
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task BroadcastMessage()
        {
            var random = new Random();
            var hubName = _hubNames[random.Next(_hubNames.Length)];
            var url = GetBroadcastUrl(hubName);
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url));

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _serviceUtils.GenerateAccessToken(url, _serverName));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var payloadRequest = new PayloadMessage
            {
                Target = _target,
                Arguments = new[]
                {
                    _serverName,
                    $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                }
            };
            request.Content = new StringContent(JsonConvert.SerializeObject(payloadRequest), Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        private Uri GetUrl(string baseUrl)
        {
            return new UriBuilder(baseUrl).Uri;
        }

        private string GetBroadcastUrl(string hubName)
        {
            return $"{GetBaseUrl(hubName)}";
        }

        private string GetSendToUserUrl(string hubName, string userId)
        {
            return $"{GetBaseUrl(hubName)}/user/{userId}";
        }

        private string GetBaseUrl(string hubName)
        {
            return $"{_endpoint}:5002/api/v1-preview/hub/{hubName.ToLower()}";
        }

        private string GenerateServerName()
        {
            return $"{Environment.MachineName}_{Guid.NewGuid():N}";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _client.Dispose();
            }
        }

        private class PayloadMessage
        {
            public string Target { get; set; }

            public object[] Arguments { get; set; }
        }
    }

    
}
