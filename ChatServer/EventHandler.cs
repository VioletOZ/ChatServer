﻿using System;
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
    class EventHandler : WebSocketBehavior
    {
        private string _suffix;
        private BlockingCollection<string> _blockQueue;
        //private ConnectionMultiplexer _multiPlexer;        
        // Redis 구독메시지올경우 메시지 처리
        private Action<RedisChannel, RedisValue> m_onRedisMessageHandler = null;

        public Action<RedisChannel, RedisValue> OnRedisMessageHandler
        {
            get
            {
                if (this.m_onRedisMessageHandler == null)
                {
                    this.m_onRedisMessageHandler = new Action<RedisChannel, RedisValue>
                                                ((channel, value) => /*this.blockQueue.Add(value)*/
                                                SendAsync("SubHandler Val : " + value, null));
                }
                return this.m_onRedisMessageHandler;
            }
        }

        public ChatPlayer m_ChatPlayer;
        public Message m_Message;

        public string Channel {get; set;}
        public string GuildChannel { get; set; }

        public EventHandler()
        {
            this._suffix = String.Empty;
            this._blockQueue = new BlockingCollection<string>();

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
            string guild = this.Headers.Get("Guild") ?? "Test_Guild";                  //Guild - Redis에 없는듯하여 클라에서 받아옴

            InitClient(sessionID, UID, name, guild);

            Console.WriteLine("Connected : " + ID + "(" + UID + ") Count : " + Sessions.Count);

        }

        protected override void OnMessage(MessageEventArgs args)
        {
            Console.WriteLine("OnMessage ID : " + ID + "\nMsg : " + args.Data);
            if (args.Data == null)
            {
                Console.WriteLine("Chat OnMessage Data Null");
                return;
            }

            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(args.Data));
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            m_Message = JsonSerializer.Deserialize<Message>(stream, options);

            switch(m_Message.Command)
            {
                case CHAT_COMMAND.CHAT:
                    if (!m_ChatPlayer.SendMessage(m_Message))
                        SendAsync(ERROR_CODE.NULLDATA.ToString(), null);
                    break;

                case CHAT_COMMAND.CHANGE_CHANNEL:
                    if (!m_ChatPlayer.ChangeChannel(m_Message.Channel))
                        SendAsync(ERROR_CODE.NULLDATA.ToString(), null);
                    break;

                case CHAT_COMMAND.ENTER_GUILD_CHANNEL:
                    if (!m_ChatPlayer.EnterGuildChannel(m_Message.Channel))
                        SendAsync(ERROR_CODE.NULLDATA.ToString(), null);
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

        public void ChatProcAdd(Message message)
        {
            Console.WriteLine("Func MessageProcAdd");

            //_blockQueue.Add(new Message
            //{
            //    Type = message.Type,
            //    Text = message.Text
            //}.Serialize());

            // Redis Publish
            _ = RedisManager.Instance.Publish(message);
        }

        public void ChatprocGet()
        {
            Console.WriteLine("Func MessageProcGet");
            foreach ( string item in _blockQueue.GetConsumingEnumerable())
            {
                //Sessions.BroadcastAsync(item.Serialize(), null);
                Console.Write(item.Serialize(), null);
            }
        }

        public void RunTask(Message message)
        {
            Console.WriteLine("Func RunTask");
            //this._outTask = Task.Run(() => ChatprocGet());
            Task.Run(() => ChatProcAdd(message));
        }

        public void TargetSend(string sessionID, string message)
        {
            Console.WriteLine("TargetID - " + sessionID + " : " + message);
            SendAsync(message, null);
        }

        public async void InitClient(string sessionId, string uid, string name, string guild)
        {
            if (m_ChatPlayer != null)
                Console.WriteLine("Chat OnOpen ChatPlayer not null : " + ID);

            // 접속시에 유저정보 세팅
            m_ChatPlayer = new ChatPlayer(sessionId, uid, name, guild);

            var result = await RedisManager.Instance.AuthVerify(sessionId);
            if (!result)
            {
                Console.WriteLine("세션 인증실패 - SessionID : " + sessionId + " " + uid + name);
                Close(CloseStatusCode.InvalidData, "InvalidData");
                return;
            }

            // 접속시에 일반 채널 구독
            string channel = CHAT_TYPE.NORMAL.ToString() + "1";             //채널별 인원수 및 최대채널 등을 고려하여 임시선정필요
            _ = RedisManager.Instance.Subscribe(channel, ID);
            m_ChatPlayer.EnterChannel(channel, ID);

            Console.WriteLine("========================================");
            _ = RedisManager.Instance.SubscribeAction(channel, OnRedisMessageHandler);

            // 길드가 있을경우 길드도 구독
            if (string.IsNullOrEmpty(guild))
            {
                string guildChannel = CHAT_TYPE.GUILD.ToString() + guild;
                _ = RedisManager.Instance.Subscribe(guildChannel, ID);
            }
            
        }
        
    }
}
