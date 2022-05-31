using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebSocketSharp;
using WebSocketSharp.Server;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using StackExchange.Redis;
using System.Text;
using System.IO;

namespace ChatServer
{
    class Chat : WebSocketBehavior
    {
        private string _suffix;
        // Redis 구독메시지올경우 메시지 처리
        private Action<RedisChannel, RedisValue> m_onRedisMessageHandler = null;

        public Action<RedisChannel, RedisValue> OnRedisMessageHandler
        {
            get
            {
                if (this.m_onRedisMessageHandler == null)
                {
                    this.m_onRedisMessageHandler = new Action<RedisChannel, RedisValue>
                                                ((channel, value) =>
                                                    {
                                                        Console.WriteLine(value.ToString());
                                                        SendAsync(value.ToString(), null);

                                                    });
                }
                return this.m_onRedisMessageHandler;
            }
        }

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters =
                {
                    new JsonStringEnumConverter()
                }
        };

        //private BlockingCollection<string> _blockQueue;
        public MessageQueue m_MessageQueue;
        public ChatPlayer m_ChatPlayer;

        public Chat()
        {
            this._suffix = String.Empty;
            this.m_MessageQueue = new MessageQueue();

            this.m_ChatPlayer = null;
        }

        public string Suffix
        {
            get
            {
                return _suffix;
            }

            set
            {
                _suffix = value ?? String.Empty;
            }
        }

        // Socket 연결시 호출
        protected override async void OnOpen()
        {
            // Redis 에서 해당유저 UID로 세션 검색.
            string sessionID = this.Headers.Get("SessionID") ?? null;               //SessionID
            string UID = this.Headers.Get("UserUID") ?? null;                       //UserUID
            string name = this.Headers.Get("UserName") ?? null;                     //Name - Redis에 없음
            int guildID = 0;
            Int32.TryParse(this.Headers.Get("GuildID"), out guildID);
            
            
            var result = await InitClient(sessionID, UID, name, guildID);
            if (!result)
            {
                Console.WriteLine("Chat OnOpen InitClient Error");
            }

            res_ChatLogin resLogin = new res_ChatLogin();
            resLogin.ReturnCode = RETURN_CODE.RC_OK;
            resLogin.ChannelID = m_ChatPlayer.NormalChannel;
            resLogin.GuildChannelID = m_ChatPlayer.GuildChannel;

            string json = JsonSerializer.Serialize<res_ChatLogin>(resLogin, options);
            SendAsync(json, null);
            Console.WriteLine("Connected : " + ID + "(UID:" + UID + ") Count : " + Sessions.Count);

        }

        protected override async void OnMessage(MessageEventArgs args)
        {
            Console.WriteLine("OnMessage ID : " + ID + "\nMsg : " + args.Data);
            if (args.Data == null)
            {
                Console.WriteLine("Chat OnMessage Data Null");
                return;
            }

            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(args.Data));
            req_Command command = JsonSerializer.Deserialize<req_Command>(stream, options);

            if (command == null)
            {
                Console.WriteLine("Chat OnMessage Command Null");
                return;
            }

            stream = new MemoryStream(Encoding.UTF8.GetBytes(args.Data));
            
            // 
            switch (command.Command)
            {
                case CHAT_COMMAND.CT_LOGIN:
                    break;

                case CHAT_COMMAND.CT_RECONNECT:                    
                    m_ChatPlayer.Reconnect(JsonSerializer.Deserialize<req_ChatReConnect>(stream, new JsonSerializerOptions()));
                    break;

                case CHAT_COMMAND.CT_LOGOUT:
                    m_ChatPlayer.LogOut(JsonSerializer.Deserialize<req_ChatLogout>(stream, new JsonSerializerOptions()));
                    break;

                case CHAT_COMMAND.CT_INFO:
                    m_ChatPlayer.ChannelInfo(JsonSerializer.Deserialize<req_ChatInfo>(stream, new JsonSerializerOptions()));
                    break;

                case CHAT_COMMAND.CT_GUILD_LOG:
                    m_ChatPlayer.GuildChatLog(JsonSerializer.Deserialize<req_ChatGuildLog>(stream, new JsonSerializerOptions()));
                    break;

                case CHAT_COMMAND.CT_CHANGE_CHANNEL:
                    if (!await m_ChatPlayer.ChangeChannel(JsonSerializer.Deserialize<req_ChatChangeChannel>(stream, new JsonSerializerOptions()), OnRedisMessageHandler))
                        SendAsync(RETURN_CODE.RC_FAIL.ToString(), null);
                    break;

                case CHAT_COMMAND.CT_ENTER_CHANNEL:
                    req_ChatEnterChannel message = JsonSerializer.Deserialize<req_ChatEnterChannel>(stream, new JsonSerializerOptions());
                    if (!await m_ChatPlayer.EnterChannel(message.ChatType, message.ChannelID, OnRedisMessageHandler))
                        SendAsync(RETURN_CODE.RC_FAIL.ToString(), null);
                    break;

                case CHAT_COMMAND.CT_LEAVE_CHANNEL:
                    m_ChatPlayer.LeaveChannel(JsonSerializer.Deserialize<req_ChatLeaveChannel>(stream, new JsonSerializerOptions()));
                    break;

                case CHAT_COMMAND.CT_MESSAGE:
                    
                    m_ChatPlayer.SendMessage(JsonSerializer.Deserialize<req_ChatMessage>(stream, new JsonSerializerOptions()));
                    //if (!m_ChatPlayer.SendMessage(message))
                        //SendAsync(RETURN_CODE.RC_FAIL.ToString(), null);
                    break;

                case CHAT_COMMAND.CT_LEADER_CHANGE:
                    m_ChatPlayer.LeaderChange(JsonSerializer.Deserialize<req_ChatLeaderChange>(stream, new JsonSerializerOptions()));
                    break;

                case CHAT_COMMAND.CT_GACHA_NOTICE:
                    m_ChatPlayer.GaChaNotice(JsonSerializer.Deserialize<req_ChatGachaNotice>(stream, new JsonSerializerOptions()));
                    break;

                default:
                    Console.WriteLine("Request Command Error : " + command.Command);
                    break;
            }
        }

        protected override void OnClose(CloseEventArgs args)
        {
            // 채팅서버만 끊길 경우를 대비해 재접속 관련 코드 추가 필요.
            // 레디스에서 세션확인 해야함
            // 지금은 일단 그냥 접속종료

            Console.WriteLine("OnClose : " + ID);

            res_ChatMessage resChatMessage = new res_ChatMessage();
            resChatMessage.ReturnCode = RETURN_CODE.RC_OK;
            resChatMessage.ChatType = CHAT_TYPE.CT_SYSTEM;
            resChatMessage.ChannelID = m_ChatPlayer.channelDict[CHAT_TYPE.CT_NORMAL];
            resChatMessage.LogData = new ChatLogData();

            Sessions.BroadcastAsync(m_ChatPlayer.Name + "님이 퇴장하셨습니다.", null);

            CloseAsync();
            Console.WriteLine("Session Close Count : " + Sessions.Count);            
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine("OnError : " + e.Message);
            base.OnError(e);
        }


        public async Task<bool> InitClient(string sessionId, string uid, string name, int guildID = 0)
        {
            if (m_ChatPlayer != null)
                Console.WriteLine("Chat InitClient ChatPlayer not null : " + ID);

            // 접속시에 유저정보 세팅
            m_ChatPlayer = new ChatPlayer(sessionId, uid, name);

            var result = await m_ChatPlayer.AuthVerify();
            if (!result)
            {
                Console.WriteLine("세션 인증실패 - SessionID : " + sessionId + " " + uid + name);
                Close(CloseStatusCode.ServerError, "InvalidData");
                return false;
            }

            // 접속시에 일반 채널 구독            
            int channelNum = 1;            
            result = await m_ChatPlayer.EnterChannel(CHAT_TYPE.CT_NORMAL, channelNum, OnRedisMessageHandler);
            if(!result)
            {
                Console.WriteLine("Chat InitClient Redis Subscribe Fail : " + ID);
                Close(CloseStatusCode.ServerError, "Sbuscribe");
            }

            // 길드가 있을경우 길드도 구독
            if (guildID > 0)
            {
                // 길드UID를 채널번호로 지정 . 일단임시로
                await m_ChatPlayer.EnterChannel(CHAT_TYPE.CT_GUILD, guildID, OnRedisMessageHandler);
            }

            return true;
        }
    
    }
}

