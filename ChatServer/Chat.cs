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
            int charID = 0;
            Int32.TryParse(this.Headers.Get("FavoriteCharacterID"), out charID);

            if (sessionID == null || UID == null || name == null)
            {
                Console.WriteLine("Chat OnOpen Request Header Error");
                CloseAsync();
                return;
            }
            
            var result = await InitClient(sessionID, UID, name, charID, guildID);
            if (!result)
            {
                Console.WriteLine("Chat OnOpen InitClient Error");
                CloseAsync();
                return;
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

            MemoryStream stream;
            req_Command command = new req_Command();
            try
            {
                stream = new MemoryStream(Encoding.UTF8.GetBytes(args.Data));
                command = JsonSerializer.Deserialize<req_Command>(stream, options);
            }
            catch (Exception e)
            {
                Console.WriteLine("Chat OnMessage Json Parse Error : " + e.Message);
                SendAsync("Send Data Error", null);
                return;
            }
            

            if (command == null)
            {
                Console.WriteLine("Chat OnMessage Command Null");
                return;
            }

            stream = new MemoryStream(Encoding.UTF8.GetBytes(args.Data));
            string channel;
            // 
            switch (command.Command)
            {
                case CHAT_COMMAND.CT_LOGIN:
                    break;

                case CHAT_COMMAND.CT_RECONNECT:                    
                    m_ChatPlayer.Reconnect(JsonSerializer.Deserialize<req_ChatReConnect>(stream, new JsonSerializerOptions()));
                    break;

                case CHAT_COMMAND.CT_LOGOUT:
                    //m_ChatPlayer.LogOut(JsonSerializer.Deserialize<req_ChatLogout>(stream, new JsonSerializerOptions()));
                    //정상 종료가 있을까?? 종료는 OnClose 로 넘긴다
                    // 필요시에 종료 Code 와 Reason 작성.
                    this.OnClose(null);
                    break;

                case CHAT_COMMAND.CT_INFO:
                    req_ChatInfo infoMessage = JsonSerializer.Deserialize<req_ChatInfo>(stream, new JsonSerializerOptions());
                    //m_ChatPlayer.ChannelInfo(infoMessage);
                    //
                    if (CHAT_TYPE.CT_NORMAL == infoMessage.ChatType)
                        channel = m_ChatPlayer.GetNormalChannel();
                    else
                        channel = m_ChatPlayer.GetGuildChannel();

                    res_ChatInfo info = new res_ChatInfo();
                    List<ChatUserData> userData = RedisManager.Instance.GetUsersByChannel(channel);
                    if (userData == null)
                        info.ReturnCode = RETURN_CODE.RC_FAIL;
                    else
                        info.ReturnCode = RETURN_CODE.RC_OK;

                    info.Command = CHAT_COMMAND.CT_INFO;
                    info.ChatType = infoMessage.ChatType;
                    info.ChannelID = infoMessage.ChannelID;
                    info.ChannelUserDataList = userData;

                    SendAsync(JsonSerializer.Serialize<res_ChatInfo>(info, options), null);
                    break;

                case CHAT_COMMAND.CT_GUILD_LOG:
                    m_ChatPlayer.GuildChatLog(JsonSerializer.Deserialize<req_ChatGuildLog>(stream, new JsonSerializerOptions()));
                    break;

                case CHAT_COMMAND.CT_CHANGE_CHANNEL:
                    res_ChatChangeChannel changeChannel = new res_ChatChangeChannel();
                    if (!await m_ChatPlayer.ChangeChannel(JsonSerializer.Deserialize<req_ChatChangeChannel>(stream, new JsonSerializerOptions()), 
                                                            m_ChatPlayer.userData, OnRedisMessageHandler))
                        changeChannel.ReturnCode = RETURN_CODE.RC_FAIL;
                    else
                        changeChannel.ReturnCode = RETURN_CODE.RC_OK;

                    changeChannel.Command = command.Command;
                    changeChannel.ChannelID = m_ChatPlayer.NormalChannel;
                    changeChannel.ChannelUserDataList = RedisManager.Instance.GetUsersByChannel(m_ChatPlayer.GetNormalChannel());

                    SendAsync(JsonSerializer.Serialize<res_ChatChangeChannel>(changeChannel, options), null);
                    break;

                case CHAT_COMMAND.CT_ENTER_CHANNEL:
                    req_ChatEnterChannel enterMessage = JsonSerializer.Deserialize<req_ChatEnterChannel>(stream, new JsonSerializerOptions());
                    res_ChatEnterChannel enterChannel = new res_ChatEnterChannel();
                    if (!await m_ChatPlayer.EnterChannel(enterMessage.ChatType, enterMessage.ChannelID, OnRedisMessageHandler))
                        enterChannel.ReturnCode = RETURN_CODE.RC_FAIL;
                    else
                        enterChannel.ReturnCode = RETURN_CODE.RC_OK;


                    if (CHAT_TYPE.CT_NORMAL == enterMessage.ChatType)
                        channel = m_ChatPlayer.GetNormalChannel();
                    else
                        channel = m_ChatPlayer.GetGuildChannel();

                    enterChannel.Command = CHAT_COMMAND.CT_ENTER_CHANNEL;
                    enterChannel.ChannelID = enterMessage.ChannelID;                    
                    enterChannel.ChannelUserDataList = RedisManager.Instance.GetUsersByChannel(channel);

                    SendAsync(JsonSerializer.Serialize<res_ChatEnterChannel>(enterChannel, options), null);
                    break;

                case CHAT_COMMAND.CT_LEAVE_CHANNEL:
                    res_ChatLeaveChannel leaveChannel = new res_ChatLeaveChannel();
                    if (!await m_ChatPlayer.LeaveChannel(JsonSerializer.Deserialize<req_ChatLeaveChannel>(stream, new JsonSerializerOptions())))
                        leaveChannel.ReturnCode = RETURN_CODE.RC_FAIL;
                    else
                        leaveChannel.ReturnCode = RETURN_CODE.RC_OK;

                    leaveChannel.Command = CHAT_COMMAND.CT_LEAVE_CHANNEL;

                    SendAsync(JsonSerializer.Serialize<res_ChatLeaveChannel>(leaveChannel, options), null);
                    break;

                case CHAT_COMMAND.CT_MESSAGE:
                    m_ChatPlayer.SendMessage(JsonSerializer.Deserialize<req_ChatMessage>(stream, new JsonSerializerOptions()));
                    
                    break;

                case CHAT_COMMAND.CT_LEADER_CHANGE:
                    req_ChatLeaderChange leaderMessage = JsonSerializer.Deserialize<req_ChatLeaderChange>(stream, new JsonSerializerOptions());
                    res_ChatLeaderChange leaderChange = new res_ChatLeaderChange();
                    if (!await m_ChatPlayer.LeaderChange(leaderMessage))
                        leaderChange.ReturnCode = RETURN_CODE.RC_FAIL;
                    else
                        leaderChange.ReturnCode = RETURN_CODE.RC_OK;

                    leaderChange.Command = CHAT_COMMAND.CT_LEADER_CHANGE;
                    leaderChange.LeaderCharacterID = leaderMessage.LeaderCharacterID;

                    SendAsync(JsonSerializer.Serialize<res_ChatLeaderChange>(leaderChange, options), null);
                    break;

                case CHAT_COMMAND.CT_GACHA_NOTICE:
                    req_ChatGachaNotice gaChamessage = JsonSerializer.Deserialize<req_ChatGachaNotice>(stream, new JsonSerializerOptions());
                    m_ChatPlayer.GachaNotice(gaChamessage);
                    
                    res_ChatGachaNotice gachaNotice = new res_ChatGachaNotice();
                    gachaNotice.Command = CHAT_COMMAND.CT_GACHA_NOTICE;
                    gachaNotice.ReturnCode = RETURN_CODE.RC_OK;
                    gachaNotice.UserName = m_ChatPlayer.Name;
                    gachaNotice.ItemID = gaChamessage.ItemID;
                    gachaNotice.CharID = gaChamessage.CharID;

                    //Sessions.BroadcastAsync(JsonSerializer.Serialize<res_ChatGachaNotice>(gachaNotice, options), null);

                    RedisManager.Instance.GachaPublish(m_ChatPlayer.GetNormalChannel(), gachaNotice);
                    RedisManager.Instance.GachaPublish(m_ChatPlayer.GetGuildChannel(), gachaNotice);
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
            try
            {
                // 
                m_ChatPlayer.LeaveAllChannel();
            }
            catch 
            {
                
            }

            CloseAsync();
            Console.WriteLine("Session Close Count : " + Sessions.Count);            
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine("OnError : " + e.Message);
            base.OnError(e);
        }


        public async Task<bool> InitClient(string sessionId, string uid, string name, int charId, int guildID = 0)
        {
            if (m_ChatPlayer != null)
                Console.WriteLine("Chat InitClient ChatPlayer not null : " + ID);

            // 접속시에 유저정보 세팅
            m_ChatPlayer = new ChatPlayer(sessionId, uid, name, charId);

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
                // 길드UID를 채널번호로 지정 . 
                await m_ChatPlayer.EnterChannel(CHAT_TYPE.CT_GUILD, guildID, OnRedisMessageHandler);
            }


            return true;
        }
    
    }
}

