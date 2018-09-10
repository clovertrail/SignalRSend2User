// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.SignalR.Samples.Serverless
{
    public class ClientHandler
    {
        private List<HubConnection> _connectionList;
        private Counter _counter;
        private string _target = ServiceUtils.MethodName;

        public ClientHandler(string connectionString, string hubName, int clientCount, Counter counter)
        {
            _counter = counter;
            var serviceUtils = new ServiceUtils(connectionString);
            _connectionList = new List<HubConnection>(clientCount);
            for (var i = 0; i < clientCount; i++)
            {
                var url = GetClientUrl(serviceUtils.Endpoint, hubName);
                // generate random userId
                var userId = $"{ServiceUtils.ClientUserIdPrefix}{i}";
                var connection = new HubConnectionBuilder()
                .WithUrl(url, option =>
                {
                    option.AccessTokenProvider = () =>
                    {
                        return Task.FromResult(serviceUtils.GenerateAccessToken(url, userId));
                    };
                }).Build();
                connection.Closed += e =>
                {
                    Console.WriteLine($"connection was closed: {e.Message}");
                    return Task.CompletedTask;
                };
                connection.On<string, string>(_target,
                (string server, string timestamp) =>
                {
                    _counter.Latency(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - Convert.ToInt64(timestamp));
                    _counter.RecordRecvSize(_target.Length + 8);
                });
                _connectionList.Add(connection);
            }
        }

        public async Task StartAsync()
        {
            try
            {
                var taskList = new List<Task>(_connectionList.Count);
                for (var i = 0; i < _connectionList.Count; i++)
                {
                    taskList.Add(_connectionList[i].StartAsync());
                }
                await Task.WhenAll(taskList);
            }
            catch (Exception e)
            {
                Console.WriteLine($"error when connecting: {e.Message}");
            }
        }

        public async Task DisposeAsync()
        {
            var taskList = new List<Task>(_connectionList.Count);
            for (var i = 0; i < _connectionList.Count; i++)
            {
                taskList.Add(_connectionList[i].DisposeAsync());
            }
            await Task.WhenAll(taskList);
        }

        private string GetClientUrl(string endpoint, string hubName)
        {
            return $"{endpoint}:5001/client/?hub={hubName}";
        }
    }
}
