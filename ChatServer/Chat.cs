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
        //private BlockingCollection<string> _blockQueue;
        public MessageQueue m_MessageQueue;

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
                                                        try
                                                        {
                                                            string message = DateTime.Now.ToString() + ":" + value.ToString();
                                                            SendAsync(message, null);
                                                        }
                                                        catch(Exception e)
                                                        {
                                                            Console.WriteLine("OnRedisMessageHandler Exception : " + e.Message);
                                                            m_ChatPlayer.LeaveAllChannel();
                                                            Console.WriteLine("LeaveChannel : " + m_ChatPlayer.SessionID);
                                                            CloseAsync(CloseStatusCode.ServerError, e.Message);
                                                        }
                                                    });
                }
                return this.m_onRedisMessageHandler;
            }
        }

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
        };


        public ChatPlayer m_ChatPlayer;
        public Message m_Message;

        public string Channel {get; set;}
        public string GuildChannel { get; set; }

        public Chat()
        {
            this._suffix = String.Empty;
            this.m_MessageQueue = new MessageQueue();

            this.m_ChatPlayer = null;
            this.m_Message = new Message();
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
        protected override void OnOpen()
        {
            // Redis 에서 해당유저 UID로 세션 검색.
            string sessionID = this.Headers.Get("SessionID") ?? null;          //SessionID
            string UID = this.Headers.Get("UID") ?? null;                      //UserUID
            string name = this.Headers.Get("Name") ?? null;                    //Name - Redis에 없는듯하여 클라에서 받아옴
            string guild = this.Headers.Get("Guild") ?? "Test_Guild";          //Guild - Redis에 없는듯하여 클라에서 받아옴

            InitClient(sessionID, UID, name);

            ResMessage resMessage = new ResMessage();
            resMessage.Status = ERROR_CODE.SUCCESS;
            resMessage.Type = CHAT_TYPE.SYSTEM;
            resMessage.Channel = 1;
            resMessage.Text = "OnOpen";

            SendAsync(JsonSerializer.Serialize<ResMessage>(resMessage, options), null);
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
            
            m_Message = JsonSerializer.Deserialize<Message>(stream, options);

            // 
            switch (m_Message.Command)
            {
                case CHAT_COMMAND.CHAT:
                    if (!m_ChatPlayer.SendMessage(m_Message))
                        SendAsync(ERROR_CODE.ERROR.ToString(), null);
                    break;

                case CHAT_COMMAND.CHANGE_CHANNEL:
                    if(!await m_ChatPlayer.ChangeChannel(m_Message.Channel, OnRedisMessageHandler))
                        SendAsync(ERROR_CODE.ERROR.ToString(), null);
                    break;

                case CHAT_COMMAND.ENTER_GUILD_CHANNEL:
                    if (!m_ChatPlayer.EnterGuildChannel(m_Message.Type))
                        SendAsync(ERROR_CODE.ERROR.ToString(), null);
                    break;

                case CHAT_COMMAND.REPORT:
                    m_ChatPlayer.ReportMessage();
                    SendAsync("Report", null);
                    break;
            }
        }

        protected override void OnClose(CloseEventArgs args)
        {
            // 채팅서버만 끊길 경우를 대비해 재접속 관련 코드 추가 필요.
            // 레디스에서 세션확인 해야함
            // 지금은 일단 그냥 접속종료

            Console.WriteLine("OnClose : " + ID);

            ResMessage resMessage = new ResMessage();
            resMessage.Status = ERROR_CODE.SUCCESS;
            resMessage.Type = CHAT_TYPE.SYSTEM;
            resMessage.Channel = m_ChatPlayer.channelDict[CHAT_TYPE.NORMAL];
            resMessage.Text = "OnOpen";

            Sessions.BroadcastAsync(m_ChatPlayer.Name + "님이 퇴장하셨습니다.", null);

            try
            {
                CloseAsync();
                Console.WriteLine("Session Close Count : " + Sessions.Count);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error : " + e.Message);
                CloseAsync(CloseStatusCode.ServerError, e.Message);
            }
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine("/////////////////////////////////////" + e.Message);
            base.OnError(e);
        }

        public void TargetSend(string sessionID, string message)
        {
            Console.WriteLine("TargetID - " + sessionID + " : " + message);
            SendAsync(message, null);
        }

        public async void InitClient(string sessionId, string uid, string name)
        {
            if (m_ChatPlayer != null)
                Console.WriteLine("Chat OnOpen ChatPlayer not null : " + ID);

            // 접속시에 유저정보 세팅
            m_ChatPlayer = new ChatPlayer(sessionId, uid, name);

            var result = await m_ChatPlayer.AuthVerify();
            if (!result)
            {
                Console.WriteLine("세션 인증실패 - SessionID : " + sessionId + " " + uid + name);
                Close(CloseStatusCode.InvalidData, "InvalidData");
                return;
            }

            // 접속시에 일반 채널 구독            
            int channelNum = 1;            
            m_ChatPlayer.EnterChannel(CHAT_TYPE.NORMAL, channelNum, OnRedisMessageHandler);

            string guild = Constance.GUILD;
            // 길드가 있을경우 길드도 구독
            if (string.IsNullOrEmpty(guild))
            {
                // 길드UID를 채널번호로 지정 . 일단임시로
                int guildUID = 1;
                m_ChatPlayer.EnterChannel(CHAT_TYPE.GUILD, guildUID, OnRedisMessageHandler);
            }

        }
        
    }
}
