using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace ChatServer
{
    // 채널서버에 접속한 개별 플레이어 관리
    class ChatPlayer
    {
        public string SessionID { get; set; }
        public int GuildID { get; set; }                       // 
        public Dictionary<CHAT_TYPE, int> channelDict = new Dictionary<CHAT_TYPE, int>();        // 현재 참여중인 채널 <채널이름, 채널번호>

        public int NormalChannel { get; set; }              // 쓰기편하려고.. 
        public int GuildChannel { get; set; }

        public ChatUserData UserData = new ChatUserData();
        public SessionState SessionState = new SessionState();
        
        // 1:1 대화등의 메시지 저장용
        public Dictionary<string, BlockingCollection<string>> WhisperMessage { get; set; }

        public ChatPlayer(string sessionId, long uid, string name, int guildId, int charId)
        {
            this.SessionID = sessionId;
            this.GuildID = guildId;
            this.UserData.UserUID = uid;
            this.UserData.UserName = name;
            this.UserData.CharacterID = charId;

            this.SessionState.subscriber = RedisManager.Instance.GetSubscriberAsync().Result;
            this.SessionState.db = RedisManager.Instance.GetDatabaseAsync().Result;
        }

        public async Task<bool> AuthVerify()
        {
            var result = await RedisManager.Instance.AuthVerify(SessionState, SessionID);            
            if (!result)
                return false;
            return true;
        }

        public async Task SendMessage(req_ChatMessage message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer PushMessage Message Null");
                return;
            }

            switch (message.ChatType)
            {
                case CHAT_TYPE.CT_NORMAL:
                    Console.WriteLine("SendMessage Channel : " + GetNormalChannel());
                    await RedisManager.Instance.Publish(SessionState, GetNormalChannel(), message);
                    break;

                case CHAT_TYPE.CT_GUILD:
                    Console.WriteLine("SendMessage Channel : " + GetGuildChannel());
                    await RedisManager.Instance.Publish(SessionState, GetGuildChannel(), message);
                    break;

                case CHAT_TYPE.CT_SYSTEM:
                    await RedisManager .Instance.Publish(SessionState, Constance.SYSTEM, message);
                    break;

                case CHAT_TYPE.CT_GM_NOTICE:
                    await RedisManager .Instance.Publish(SessionState, Constance.GM_NOTICE, message);
                    break;
            }
        }

        public void RecvMessage(res_ChatMessage message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer RecvMessage Message Null");
                return;
            }

            //await RedisManager.Instance.UnSubscribe(message.ChannelID.ToString(), UserData.UserUID);
            //string temp = Channels[channel].Dequeue();
        }

        public bool Reconnect(req_ChatReConnect message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer Reconnect Message Null");
                return false;
            }
            return true;
        }
        
        public void LogOut(req_ChatLogout message)
        {

        }

        public void ChannelInfo(req_ChatInfo message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer ChannelInfo Message Null");
                return;
            }

            switch (message.ChatType)
            {
                case CHAT_TYPE.CT_NORMAL:

                    break;

                case CHAT_TYPE.CT_GUILD:
                    break;

                case CHAT_TYPE.CT_MAX:
                    break;

                default:
                    break;
            }
        }

        public async Task<List<ChatLogData>> GetGuildLog()
        {
            return await RedisManager.Instance.GetGuildLogData(SessionState, GetGuildChannel());
        }

        // 채널변경은 일반 채널밖에 되지않음.
        public async Task<bool> ChangeChannel(req_ChatChangeChannel message, ChatUserData userData, Action<RedisChannel, RedisValue> action)
        {
            // 변경 전 채널
            int beforeChannelNum = NormalChannel;
            string beforeChannel = GetNormalChannel();

            if (message.ChannelID == beforeChannelNum)
                return false;

            // 기존 채널 정보 변경후에
            channelDict[CHAT_TYPE.CT_NORMAL] = message.ChannelID;
            Console.WriteLine("Change Channel : " + channelDict[CHAT_TYPE.CT_NORMAL]);

            // 구독중인 채널 삭제하고
            //await RedisManager.Instance.UnSubscribe(beforeChannel, userData);
            if (!await LeaveChannel(message.ChatType))
                Console.WriteLine("ChatPlayer LeaveChannel Enter Fail : " + message.ChatType);

            // 다시 구독요청
            Console.WriteLine("ChangeChannel : " + CHAT_TYPE.CT_NORMAL.ToString() + message.ChannelID);
            if (!await EnterChannel(CHAT_TYPE.CT_NORMAL, message.ChannelID, action))
                Console.WriteLine("ChatPlayer ChannelChange Enter Fail : " + CHAT_TYPE.CT_NORMAL);

            return true;
        }

        public async Task<bool> EnterChannel(CHAT_TYPE type, int channelNum, Action<RedisChannel, RedisValue> action)
        {
            if (!channelDict.ContainsKey(type))
                channelDict.Add(type, channelNum);
            
            string ch = "";
            switch (type)
            {
                case CHAT_TYPE.CT_NORMAL:
                    NormalChannel = channelNum;
                    ch = GetNormalChannel();
                    break;

                case CHAT_TYPE.CT_GUILD:
                    GuildChannel = channelNum;
                    ch = GetGuildChannel();
                    break;
                case CHAT_TYPE.CT_SYSTEM:
                    ch = Constance.SYSTEM;
                    break;
                case CHAT_TYPE.CT_GM_NOTICE:
                    ch = Constance.GM_NOTICE;
                    break;
            }

            //await UserStateChannel(CHAT_ENTER_STATE.CT_ENTER, type, ch);
            if (!await EnterUserChannel(type))
                Console.WriteLine("ChatPlayer EnterUserChannel Fail : " + UserData.UserUID);

            Console.WriteLine("EnterChannel : " + ch);
            if (!await RedisManager.Instance.SubscribeAction(SessionState, ch, UserData, action))
                Console.WriteLine("ChatPlayer EnterChannel Subscribe Fail : " + ch);

            return true;
        }

        public async Task<bool> LeaveChannel(CHAT_TYPE type)
        {
            Console.WriteLine("LeaveChannel :" + type);
            string channel = "";
            switch (type)
            {
                case CHAT_TYPE.CT_NORMAL:
                    channel = GetNormalChannel();
                    break;

                case CHAT_TYPE.CT_GUILD:
                    channel = GetGuildChannel();
                    break;
                case CHAT_TYPE.CT_SYSTEM:
                    channel = Constance.SYSTEM;
                    break;
                case CHAT_TYPE.CT_GM_NOTICE:
                    channel = Constance.GM_NOTICE;
                    break;
                default:
                    return false;
            }

            if (!await RedisManager.Instance.UnSubscribe(SessionState, channel, UserData.UserUID))
                return false;

            //await UserStateChannel(CHAT_ENTER_STATE.CT_ENTER, type, channel);
            await LeaveUserChannel(type);
            return true;
        }

        public async Task<bool> LeaderChange(req_ChatLeaderChange message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer LeaderChange Message Null");
                return false;
            }

            await Task.Run(() => UserData.CharacterID = message.LeaderCharacterID);

            return true;
        }

        public void GachaNotice(req_ChatGachaNotice message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer GachaNotice Message Null");
                return;
            }

        }


        public async Task LeaveAllChannel()
        {
            //channelDict.AsParallel().ForAll(entry => Task.Run(() => RedisManager.Instance.UnSubscribe(entry.Key.ToString() + entry.Value, this.SessionID)));
            await RedisManager.Instance.UnSubscribe(SessionState, GetNormalChannel(), UserData.UserUID);

            if (GuildChannel > 0)
                await RedisManager.Instance.UnSubscribe(SessionState, GetGuildChannel(), UserData.UserUID);

        }

        public async Task<bool> EnterUserChannel(CHAT_TYPE type)
        {
            res_ChatEnterUser enterUser = new res_ChatEnterUser();
            enterUser.Command = CHAT_COMMAND.CT_CHANNEL_ENTER_USER;
            enterUser.ReturnCode = RETURN_CODE.RC_OK;
            enterUser.ChatType = type;
            enterUser.UserData = UserData;

            switch (type)
            {
                case CHAT_TYPE.CT_NORMAL:
                    await RedisManager.Instance.ForcePublish(SessionState, GetNormalChannel(), EncodingJson.Serialize(enterUser));
                    break;
                case CHAT_TYPE.CT_GUILD:
                    await RedisManager.Instance.ForcePublish(SessionState, GetGuildChannel(), EncodingJson.Serialize(enterUser));
                    break;
                default:
                    return false;
            }
            return true;
        }

        public async Task LeaveUserChannel(CHAT_TYPE type)
        {
            res_ChatLeaveUser leaveUser = new res_ChatLeaveUser();
            leaveUser.Command = CHAT_COMMAND.CT_CHANNEL_LEAVE_USER;
            leaveUser.ReturnCode = RETURN_CODE.RC_OK;
            leaveUser.ChatType = type;
            leaveUser.UserUID = UserData.UserUID;

            switch (type)
            {
                case CHAT_TYPE.CT_NORMAL:
                    await RedisManager.Instance.ForcePublish(SessionState, GetNormalChannel(), EncodingJson.Serialize(leaveUser));
                    break;

                case CHAT_TYPE.CT_GUILD:
                    await RedisManager.Instance.ForcePublish(SessionState, GetNormalChannel(), EncodingJson.Serialize(leaveUser));
                    break;

                default:
                    return;
            }

        }

        public async Task<bool> ReceiveEnd()
        {
            if (!await RedisManager.Instance.UnSubscribe(SessionState, GetNormalChannel(), UserData.UserUID))
                return false;

            return true;
        }

        public bool ReportMessage()
        {
            //...
            return true;
        }

        public string GetNormalChannel()
        {
            return Constance.NORMAL + NormalChannel;
        }

        public string GetGuildChannel()
        {
            return Constance.GUILD + GuildChannel;
        }


    }
}
