using Microsoft.Azure.EventHubs;
using Microsoft.Azure.SignalR.Samples.Serverless;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Transceiver
{
    class Program
    {
        private const int TRANSMIT_DELAY_MS = 100;
        private static CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private static ServerHandler _server;
        private static Dictionary<string, string> _settings;
        private static string[] _hubIds;

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Specify <client>|<server>");
                Console.WriteLine("  <client> client-count: default client count is 1000");
                Console.WriteLine("  <server> target-count <server-count>: default server count is 2");
                return;
            }
            var settingsPath = Path.Combine(Environment.CurrentDirectory, "local.settings.json");
            _settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(settingsPath));

            var hubIdsPath = Path.Combine(Environment.CurrentDirectory, "HubIds.csv");
            _hubIds = File.ReadAllLines(hubIdsPath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
            if (args[0].Equals("client"))
            {
                var clientCount = 1000;
                if (args.Length == 2)
                {
                    clientCount = int.Parse(args[1]);
                }
                Task.Run(async () =>
                {
                    var counter = new Counter();
                    var client = new ClientHandler(_settings["SignalRServiceConnectionString"], ServiceUtils.HubName, clientCount, counter);
                    counter.StartPrint();
                    await client.StartAsync();
                    Console.WriteLine("Client started...");
                    Console.ReadLine();
                    await client.DisposeAsync();
                }).Wait();
            }
            else
            {
                var targetClient = 0;
                if (args.Length < 2)
                {
                    Console.WriteLine();
                }
                else
                {
                    targetClient = int.Parse(args[1]);
                }

                var threadCount = 2;
                if (args.Length == 3)
                {
                    threadCount = int.Parse(args[2]);
                }
                _server = new ServerHandler(_settings["SignalRServiceConnectionString"], _hubIds);
                // increase the concurrent connection limit.
                ServicePointManager.DefaultConnectionLimit = threadCount;
                var txThreads = Enumerable.Range(0, threadCount)
                    .Select(_ => new Thread(() => TransmitEvents(targetClient)))
                    .ToList();
                foreach (var txThread in txThreads)
                    txThread.Start();

                Thread.Sleep(TimeSpan.FromMinutes(5));
                cancellationToken.Cancel();
            }
        }

        static async void TransmitEvents(int targetClient)
        {
            while (!cancellationToken.Token.IsCancellationRequested)
            {
                await _server.SendToUsers(ServiceUtils.HubName, targetClient);
                await Task.Delay(TRANSMIT_DELAY_MS);
            }
        }
    }
}
