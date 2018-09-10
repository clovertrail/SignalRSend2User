using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace Microsoft.Azure.SignalR.Samples.Serverless
{
    public class Counter
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
        private readonly long Step = 100;    // latency unit
        private readonly long Length = 10;    // how many latency categories will be displayed

        private long[] _latency;
        private long _totalReceived;
        private long _totalRecvSize;
        private long _totalSent;
        private long _totalSentSize;

        private Timer _timer;
        private long _startPrint;
        private bool _hasRecord;

        private object _lock = new object();

        public Counter()
        {
            _latency = new long[Length];
        }

        public void Latency(long dur)
        {
            long index = dur / Step;
            if (index >= Length)
            {
                index = Length - 1;
            }
            Interlocked.Increment(ref _latency[index]);
            _hasRecord = true;
        }

        public void RecordSentSize(long sentSize)
        {
            Interlocked.Increment(ref _totalSent);
            Interlocked.Add(ref _totalSentSize, sentSize);
            _hasRecord = true;
        }

        public void RecordRecvSize(long recvSize)
        {
            Interlocked.Increment(ref _totalReceived);
            Interlocked.Add(ref _totalRecvSize, recvSize);
            _hasRecord = true;
        }

        public void StartPrint()
        {
            if (Interlocked.CompareExchange(ref _startPrint, 1, 0) == 0)
            {
                _timer = new Timer(Report, state: this, dueTime: Interval, period: Interval);
            }
        }

        public void StopPrint()
        {
            Interlocked.CompareExchange(ref _startPrint, 0, 1);
        }

        private void Report(object state)
        {
            if (_hasRecord && Interlocked.Read(ref _startPrint) == 1)
            {
                ((Counter)state).InternalReport();
                _hasRecord = false;
            }
        }

        private void InternalReport()
        {
            var dic = new ConcurrentDictionary<string, long>();
            var batchMessageDic = new ConcurrentDictionary<string, long>();
            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < Length; i++)
            {
                if (_latency[i] != 0)
                {
                    sb.Clear();
                    var label = Step + i * Step;
                    if (i < Length - 1)
                    {
                        sb.Append("message:lt:");
                    }
                    else
                    {
                        sb.Append("message:ge:");
                    }
                    sb.Append(Convert.ToString(label));
                    dic[sb.ToString()] = _latency[i];
                }
            }
            if (Interlocked.Read(ref _totalSent) != 0)
            {
                dic["message:sent"] = Interlocked.Read(ref _totalSent);
            }
            if (Interlocked.Read(ref _totalReceived) != 0)
            {
                dic["message:received"] = Interlocked.Read(ref _totalReceived);
            }
            if (Interlocked.Read(ref _totalSentSize) != 0)
            {
                dic["message:sendSize"] = Interlocked.Read(ref _totalSentSize);
            }
            if (Interlocked.Read(ref _totalRecvSize) != 0)
            {
                dic["message:recvSize"] = Interlocked.Read(ref _totalRecvSize);
            }
            
            // dump out all statistics
            Console.WriteLine(JsonConvert.SerializeObject(new
            {
                Time = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ"),
                Counters = dic
            }));
        }
    }
}
