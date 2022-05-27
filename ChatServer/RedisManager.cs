using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using WebSocketSharp.Server;

namespace ChatServer
{
    class RedisManager
    {
        private static volatile RedisManager _instance;        
        private static object _syncRoot = new Object();
        public static RedisManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                            _instance = new RedisManager();
                    }
                }

                return _instance;
            }
        }

        private ISubscriber _subscriber { get; }
        private IDatabase _db { get; }
        private string _env { get; }

        private ConnectionMultiplexer _multiplexer = null;
        private Dictionary<string, List<string>> _subChannelDict = new Dictionary<string, List<string>>(); // 구독중인 채널, Client Session
        private MessageQueue _messageQueue = new MessageQueue();

        public RedisManager()
        {
            _env = Environment.GetEnvironmentVariable("RedisConnection", EnvironmentVariableTarget.Process);
            if (_env == null)
            {
                Console.WriteLine("ALARM - Localhost RedisConnection");
                _env = "localhost:6379";
            }
            _multiplexer = ConnectionMultiplexer.Connect(_env);

            _subscriber = _multiplexer.GetSubscriber();
            _db = _multiplexer.GetDatabase();      
        }

        public void RedisInit()
        {
            
        }

        // 레디스 Session 검증 - 서버에서 접속시 등록된 Session으로 허용된 접근인지 확인
        public async Task<bool> AuthVerify(string SessionID)
        {
            try
            {
                Console.WriteLine("Enter - " + SessionID);
                var result = await _db.KeyExistsAsync("Session:" + SessionID);
                if (result)
                    return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("RedisManager AuthVerify Exception : " + e.Message);
                return false;
            }

            return true;
        }

        public async Task<bool> SubscribeAction(string channel, Action<RedisChannel, RedisValue> ac)
        {
            await _multiplexer.GetSubscriber().SubscribeAsync(channel, ac);
            return true;
        }
        // Redis Subscribe - 구독하고 있는 채널에 Pub가 오면 구독중인 Client에 메시지 전달
        public async Task<bool> Subscribe(string channel, string session=null)
        {
            if (string.IsNullOrEmpty(channel)) 
                return false;

            if (!_subChannelDict.ContainsKey(channel))
            {
                _subChannelDict.Add(channel, new List<string>());
            }

            _subChannelDict[channel].Add(session); // 구독중인 채널 추가

            Console.WriteLine("Sub Channel : " + channel);

            await _multiplexer.GetSubscriber().SubscribeAsync(channel, (RedisChannel ch, RedisValue val) =>
            {
                try
                {
                    string eventMessage = EncodingJson.Serialize<string>(val);
                    if (string.IsNullOrEmpty(eventMessage))
                        eventMessage = "";

                    // 구독 받은 메시지 MessageQueue 에 저장
                    Console.WriteLine("Sub Message : " + eventMessage);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Redis Subscribe Exception : " + e.Message);
                }
            });


            return true;
        }

        // Redis Pub
        public async Task Publish(string channel, Message message)
        {
            Console.WriteLine("Publish Message Type : " + message.Type +"-" +channel);

            
            // TODO: 보내기전에 Connect 확인
            //if (!isConnected)
            //    return;

            _messageQueue.Add(message.Text);
            //string channel = message.Type + message.Channel;
            await _subscriber.PublishAsync(channel, message.Text);
            //_db.StringSet(message.Channel.ToString(), message.Text);
        }

        // 서버에서 특정채널에 메시지만 보내도록 쓸려고 만든것
        public void ForcePublish(string channel, string message)
        {
            _ = _subscriber.PublishAsync(channel, message);
        }

        public async Task UnSubscribe(string channel, string session)
        {
            await _subscriber.UnsubscribeAsync(channel);
        }

        public async Task UnSubscribeAll()
        {
            await _subscriber.UnsubscribeAllAsync();
        }

        public async Task GetUserHash(string userUID)
        {
            var val = await _db.HashGetAsync("User:" + userUID, "User");
            Console.WriteLine("GetHash" + val);
        }

        public async Task GetString(string key)
        {
            var val = await _db.StringGetAsync(key);
            Console.WriteLine("GetString : " + val);
        }
    }

}
