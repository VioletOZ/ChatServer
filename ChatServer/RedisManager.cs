using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using StackExchange.Redis.MultiplexerPool;
using WebSocketSharp.Server;

using System.Text.Json;
using System.Text.Json.Serialization;


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

        
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters =
                {
                    new JsonStringEnumConverter()
                }
        };

        private readonly ConnectionMultiplexer[] _connectionPool;
        private readonly ConfigurationOptions _redisConfigurationOptions;

        private string _env { get; }

        //private ConnectionMultiplexer _multiplexer = null;
        private Dictionary<string, List<ChatUserData>> _subChannelDict = new Dictionary<string, List<ChatUserData>>(); // 구독중인 채널, Client Session
        private MessageQueue _messageQueue = new MessageQueue();

        public ConnectionMultiplexer gameServerRedis = null;
        public SessionState gameServerState = new SessionState();


        public RedisManager()
        {
            //_env = Environment.GetEnvironmentVariable("RedisConnection", EnvironmentVariableTarget.Process);
            _env = Constance.ENV_CHAT_SERVER_REDIS_ADDR + ":" + Constance.ENV_CHAT_SERVER_REDIS_PORT;
            // 채팅서버 레디스
            if (Constance.ENV_CHAT_SERVER_REDIS_ADDR == null || Constance.ENV_CHAT_SERVER_REDIS_PORT == null)
            {
                Logger.WriteLog("ChatServer Redis Connection Fail..");
                Environment.Exit(0);
            }

            //_multiplexer = ConnectionMultiplexer.Connect(_env);

            //_subscriber = _multiplexer.GetSubscriber();
            //_db = _multiplexer.GetDatabase();


            _connectionPool = new ConnectionMultiplexer[Constance.POOL_SIZE];
            _redisConfigurationOptions = ConfigurationOptions.Parse(_env);

        }

        public bool ConnectGameServerRedis()
        {
            try
            {
                ConfigurationOptions redisConf;                

                string redisAddr = Constance.ENV_GAME_SERVER_REDIS_ADDR;
                string redisPort = Constance.ENV_GAME_SERVER_REDIS_PORT;

                // 게임서버 레디스
                if (redisAddr == null || redisPort == null)
                {
                    Logger.WriteLog("GameServer Redis Unknown Addr or Port..");
                    Environment.Exit(0);
                }


                redisConf = ConfigurationOptions.Parse(redisAddr + ":" + redisPort);
                if (redisConf == null)
                {
                    Logger.WriteLog("GameServer Redis Parse Error..");
                    Environment.Exit(0);
                }

                redisConf.AbortOnConnectFail = false;

                //this.gameServerRedis = ConnectionMultiplexer.Connect(redisConf);
                //this.gameServerState.ServerSessionID = "Main";
                //this.gameServerState.db = gameServerRedis.GetDatabase();
                //this.gameServerState.subscriber = gameServerRedis.GetSubscriber();

                if (this.gameServerRedis == null)
                {
                    Logger.WriteLog("GameServer Redis Connection Fail..");
                    Environment.Exit(0);
                }
            }
            catch (Exception)
            {
                Logger.WriteLog("GameServer Redis Error");
                Environment.Exit(0);
            }
            return true;
        }

        // 레디스 Session 검증 - 서버에서 접속시 등록된 Session으로 허용된 접근인지 확인
        public async Task<bool> AuthVerify(string SessionID)
        {
            try
            {
                Logger.WriteLog("Enter - " + SessionID);
                var result = await gameServerState.db.KeyExistsAsync("Session:" + SessionID);
                //var result = await conn.db.KeyExistsAsync("Session:" + SessionID);
                if (!result)
                    return false;
            }
            catch (Exception e)
            {
                Logger.WriteLog("RedisManager AuthVerify Exception : " + e.Message);
                return false;
            }

            return true;
        }

        public async Task<bool> SubscribeAction(SessionState conn, string channel, ChatUserData user, Action<RedisChannel, RedisValue> ac)
        {
            if (!_subChannelDict.ContainsKey(channel))
                _subChannelDict.Add(channel, new List<ChatUserData>());

            if (IsSubscribe(channel, user.UserUID))
                return true;

            _subChannelDict[channel].Add(user);

            if (conn == null)
                return false;

            await conn.subscriber.SubscribeAsync(channel, ac);
            
            return true;
        }

        public bool IsSubscribe(string channel, long uid)
        {
            foreach (var d in _subChannelDict[channel])
            {
                if (d.UserUID == uid)
                    return true;
            }
            return false;
        }
        // Redis Subscribe - 구독하고 있는 채널에 Pub가 오면 구독중인 Client에 메시지 전달
        public bool Subscribe(SessionState conn, string channel, ChatUserData user = null)
        {
            if (string.IsNullOrEmpty(channel)) 
                return false;

            if (!_subChannelDict.ContainsKey(channel))
            {
                _subChannelDict.Add(channel, new List<ChatUserData>());
            }

            _subChannelDict[channel].Add(user); // 구독중인 채널 추가

            Logger.WriteLog("Sub Channel : " + channel);

            conn.subscriber.SubscribeAsync(channel, (RedisChannel ch, RedisValue val) =>
            {
                try
                {
                    string eventMessage = EncodingJson.Serialize<string>(val);
                    if (string.IsNullOrEmpty(eventMessage))
                        eventMessage = "";

                    // 구독 받은 메시지 MessageQueue 에 저장
                    Logger.WriteLog("Sub Message : " + eventMessage);

                }
                catch (Exception e)
                {
                    Logger.WriteLog("Redis Subscribe Exception : " + e.Message);
                }
            });


            return true;
        }

        // Redis Pub
        public async Task Publish(SessionState conn, string channel, req_ChatMessage message)
        {
            Logger.WriteLog("Publish Message Type : " + message.ChatType +"-" +channel);

            // TODO: 보내기전에 Connect 확인
            //if (!isConnected)
            //    return;

            res_ChatMessage resMessage = new res_ChatMessage();
            resMessage.Command = CHAT_COMMAND.CT_MESSAGE;
            resMessage.ReturnCode = RETURN_CODE.RC_OK;
            resMessage.ChatType = message.ChatType;
            resMessage.ChannelID = message.ChannelID;
            resMessage.LogData = message.LogData;

            var publish = await conn.subscriber.PublishAsync(channel, EncodingJson.Serialize(resMessage));

            // 길드 채팅의 경우 내용저장
            // 길드채널
            //      ㄴ 채팅시간
            //              ㄴ 유저네임 : 내용
            if (CHAT_TYPE.CT_GUILD == message.ChatType)
            {
                string log = EncodingJson.Serialize(message.LogData);
                HashEntry[] hash =
                {
                    // 한국시간으로 변경.
                    new HashEntry(DateTime.Now.AddHours(9).ToString(format: "yyyyMMddHHmmss"), log)
                    //new HashEntry("data", message.LogData.UserName + message.LogData.Text)
                };

                // 길드 채팅 저장
                _ = conn.db.HashSetAsync(Constance.LOG + channel, hash);
                _ = conn.db.KeyExpireAsync(Constance.LOG, DateTime.Now.AddDays(7));
            }
            
        }

        // 서버에서 특정채널에 메시지만 보내도록 쓸려고 만든것
        public async Task ForcePublish(SessionState conn, string channel, string message)
        {
            await conn.subscriber.PublishAsync(channel, message);
        }

        public async Task GachaPublish(SessionState conn, string channel, res_ChatGachaNotice notiMessage)
        {
            await conn.subscriber.PublishAsync(channel, EncodingJson.Serialize(notiMessage));
        }

        public async Task<bool> UnSubscribe(SessionState conn, string channel, long userUid)
        {
            await conn.subscriber.UnsubscribeAsync(channel);

            if (_subChannelDict.ContainsKey(channel))
            {
                for( int i = 0; i < _subChannelDict[channel].Count; i++)
                {
                    if (_subChannelDict[channel][i].UserUID == userUid)
                    {
                        _subChannelDict[channel].RemoveAt(i);

                        // 해당채널에 아무도없으면 채널 삭제
                        if (_subChannelDict[channel].Count == 0)
                            _subChannelDict.Remove(channel);
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task UnSubscribeAll(SessionState conn)
        {
            await conn.subscriber.UnsubscribeAllAsync();
        }

        public List<ChatUserData> GetUsersByChannel(SessionState conn, string channel)
        {
            try
            {
                return _subChannelDict[channel];
            }
            catch (Exception e)
            {
                Logger.WriteLog("RedisManager GetUserByChannel subChannelDict Error :" + conn.ServerSessionID + ":" + e.Message);
                return null;
            }

        }

        public int GetPossibleChannel(SessionState conn)
        {
            int count = 1;
            string channel = Constance.NORMAL + count.ToString();
            while(Constance.CHANNEL_PLAYER_MAX > _subChannelDict[channel].Count())
            {
                channel = Constance.NORMAL + Constance.POSSIBLE_CHANNEL_NUMBER.ToString();
                Constance.POSSIBLE_CHANNEL_NUMBER++;
                if (Constance.POSSIBLE_CHANNEL_NUMBER >= Constance.CHANNEL_MAX)
                    Constance.POSSIBLE_CHANNEL_NUMBER = 1;
            }
            return count;
        }

        public List<ChatGuildLogData> GetGuildLogData(SessionState conn, string channel, DateTime loginTime)
        {
            //string pattern = loginTime.AddDays(-1).ToString(format: "yyyyMM") + "*";
            string pattern = loginTime.ToString(format:"yyyyMMdd") + "*";
            var result = conn.db.HashScan(Constance.LOG + channel, pattern, Constance.PAGE_SIZE, Constance.LOG_COUNT, 0);

            List<ChatGuildLogData> logs = new List<ChatGuildLogData>();
            ChatGuildLogData log = new ChatGuildLogData();
            foreach (HashEntry entry in result)
            {
                log = JsonSerializer.Deserialize<ChatGuildLogData>(entry.Value.ToString());
                log.Time = DateTime.ParseExact(entry.Name, "yyyyMMddHHmmss", null).ToUniversalTime();
                logs.Add(log);
            }

            return logs;
        }

        public async Task GetUserHash(SessionState conn, string userUID)
        {
            var val = await conn.db.HashGetAsync("User:" + userUID, "User");
            Logger.WriteLog("GetHash" + val);

        }

        public async Task GetString(SessionState conn, string key)
        {
            var val = await conn.db.StringGetAsync(key);
            Logger.WriteLog("GetString : " + val);

        }

        public async Task<ISubscriber> GetSubscriberAsync()
        {
            var leastPendingTasks = long.MaxValue;
            ISubscriber leastPendingDatabase = null;

            for (int i = 0; i < _connectionPool.Length; i++)
            {
                var connection = _connectionPool[i];                
                if (connection == null)
                {
                    _redisConfigurationOptions.AbortOnConnectFail = false;
                    _connectionPool[i] = await ConnectionMultiplexer.ConnectAsync(_redisConfigurationOptions);
                    if (i % 100  < 5)
                        Logger.WriteLog("Redis Subscriber Count : " + i);
                    return _connectionPool[i].GetSubscriber();
                }

                var pending = connection.GetCounters().TotalOutstanding;
                if (pending < leastPendingTasks)
                {
                    leastPendingTasks = pending;
                    leastPendingDatabase = connection.GetSubscriber();
                }
            }

            return leastPendingDatabase;
        }

        public async Task<IDatabase> GetDatabaseAsync()
        {
            var leastPendingTasks = long.MaxValue;
            IDatabase leastPendingDatabase = null;

            for (int i = 0; i < _connectionPool.Length; i++)
            {
                var connection = _connectionPool[i];

                if (connection == null)
                {
                    _connectionPool[i] = await ConnectionMultiplexer.ConnectAsync(_redisConfigurationOptions);
                    return _connectionPool[i].GetDatabase();
                }

                var pending = connection.GetCounters().TotalOutstanding;

                if (pending < leastPendingTasks)
                {
                    leastPendingTasks = pending;
                    leastPendingDatabase = connection.GetDatabase();
                }
            }

            return leastPendingDatabase;
        }
    }

}
