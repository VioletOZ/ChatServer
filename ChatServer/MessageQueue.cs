using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer
{
    class MessageQueue
    {
        private BlockingCollection<ChatLogData> _queue = new BlockingCollection<ChatLogData>();
        private CancellationTokenSource _source = new CancellationTokenSource();

        public object LockObj = new object();

        public int Count { get { return _queue.Count;  } }

        public bool IsCompleted() { return _queue.IsCompleted; }

        public void CancelTake() { _source.Cancel(); }

        public bool Add(ChatLogData logData)
        {
            try
            {
                _queue.Add(logData);
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog("MessageQueue Add Error : " + e.Message);
                return false;
            }
        }

        public bool Take(ref ChatLogData msg)
        {
            try
            {
                msg = _queue.Take();
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(e.Message);
                return false;
            }
        }
    }

    public class ProducerConsumeQueue
    {

        private BlockingCollection<string> _queue = new BlockingCollection<string>();
        private CancellationTokenSource _source = new CancellationTokenSource();

        public object LockObj = new object();

        public int Count { get { return _queue.Count; } }

        public bool IsCompleted() { return _queue.IsCompleted; }

        public void CancelTake() { _source.Cancel(); }

        public bool Add(string msg)
        {
            try
            {
                _queue.Add(msg);
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(e.Message);
                return false;
            }
        }

        public bool Take(ref string msg)
        {
            try
            {
                msg = _queue.Take();
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(e.Message);
                return false;
            }
        }
    }

    public abstract class ProducerConsumer
    {
        protected ProducerConsumeQueue _queue;

        public int ThreadId { get; private set; }
        public int ProcessedCount { get; set; }

        public ProducerConsumer(ProducerConsumeQueue queue)
        {
            _queue = queue;
        }

        protected void OnThreadStart()
        {
            ThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public abstract void ThreadRun(string data);

    }

    public class Producer : ProducerConsumer
    {
        public Producer(ProducerConsumeQueue queue) : base(queue)
        {
            _queue = queue;
        }

        public override void ThreadRun(string data)
        {
            OnThreadStart();

            while (true)
            {
                if (_queue.Add(data) == false)
                    break;

                ++ProcessedCount;

                lock (_queue.LockObj)
                {
                    //_queue.Print
                }

                Thread.Sleep(100);
            }
        }
    }

    public class Consumer : ProducerConsumer
    {
        public Consumer(ProducerConsumeQueue queue) : base(queue)
        {
            _queue = queue;
        }

        public override void ThreadRun(string data)
        {
            OnThreadStart();

            while (true)
            {
                if (_queue.Take(ref data) == false)
                    break;

                ++ProcessedCount;

                lock (_queue.LockObj)
                {
                    //_queue.Print
                }

                Thread.Sleep(100);
            }
        }
    }

    
}
