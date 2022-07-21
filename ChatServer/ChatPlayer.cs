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
        public string ID { get; set; }
        public string SessionID { get; set; }
        public int GuildID { get; set; }                       // 
        public Dictionary<CHAT_TYPE, int> channelDict = new Dictionary<CHAT_TYPE, int>();        // 현재 참여중인 채널 <채널이름, 채널번호>

        public int NormalChannel { get; set; }              // 쓰기편하려고.. 
        public int GuildChannel { get; set; }
        public DateTime LoginTime;
        public ChatUserData UserData = new ChatUserData();
        //public SessionState SessionState = new SessionState();
        
        // 1:1 대화등의 메시지 저장용
        public Dictionary<string, BlockingCollection<string>> WhisperMessage { get; set; }

        public ChatPlayer(string ID, string sessionId, long uid, string name, int guildId, int charId)
        {
            this.SessionID = sessionId;
            this.GuildID = guildId;
            this.UserData.ID = ID;
            this.UserData.UserUID = uid;
            this.UserData.UserName = name;
            this.UserData.CharacterID = charId;
            this.LoginTime = DateTime.Now;
            this.NormalChannel = 1;
            this.GuildChannel = 0;

            //this.SessionState.ServerSessionID = sessionId+":"+uid;
            //this.SessionState.subscriber = RedisManager.Instance.GetSubscriberAsync(ID).Result;
            //this.SessionState.db = RedisManager.Instance.GetDatabaseAsync(ID).Result;

        }

        public async Task<bool> AuthVerify()
        {
            var result = await RedisManager.Instance.AuthVerify(SessionID);            
            if (!result)
                return false;
            return true;
        }

        public async Task SendMessage(req_ChatMessage message)
        {
            if (message == null)
            {
                Logger.WriteLog("ChatPlayer PushMessage Message Null");
                return;
            }

            switch (message.ChatType)
            {
                case CHAT_TYPE.CT_NORMAL:
                    Logger.WriteLog("SendMessage Channel : " + GetNormalChannel());
                    await RedisManager.Instance.Publish(GetNormalChannel(), message);
                    break;

                case CHAT_TYPE.CT_GUILD:
                    Logger.WriteLog("SendMessage Channel : " + GetGuildChannel());
                    await RedisManager.Instance.Publish(GetGuildChannel(), message);
                    break;

                case CHAT_TYPE.CT_SYSTEM:
                    await RedisManager .Instance.Publish(Constance.SYSTEM, message);
                    break;

                case CHAT_TYPE.CT_GM_NOTICE:
                    await RedisManager .Instance.Publish(Constance.GM_NOTICE, message);
                    break;
            }
        }

        public void RecvMessage(res_ChatMessage message)
        {
            if (message == null)
            {
                Logger.WriteLog("ChatPlayer RecvMessage Message Null");
                return;
            }

            //await RedisManager.Instance.UnSubscribe(message.ChannelID.ToString(), UserData.UserUID);
            //string temp = Channels[channel].Dequeue();
        }

        public bool Reconnect(req_ChatReConnect message)
        {
            if (message == null)
            {
                Logger.WriteLog("ChatPlayer Reconnect Message Null");
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
                Logger.WriteLog("ChatPlayer ChannelInfo Message Null");
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

        public List<ChatGuildLogData> GetGuildLog()
        {
            return RedisManager.Instance.GetGuildLogData(GetGuildChannel(), LoginTime);
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
            Logger.WriteLog("Change Channel : " + channelDict[CHAT_TYPE.CT_NORMAL]);

            // 구독중인 채널 삭제하고
            //await RedisManager.Instance.UnSubscribe(beforeChannel, userData);
            if (!await LeaveChannel(message.ChatType))
                Logger.WriteLog("ChatPlayer LeaveChannel Enter Fail : " + message.ChatType);

            // 다시 구독요청
            Logger.WriteLog("ChangeChannel : " + CHAT_TYPE.CT_NORMAL.ToString() + message.ChannelID);
            if (!await EnterChannel(CHAT_TYPE.CT_NORMAL, message.ChannelID, action))
                Logger.WriteLog("ChatPlayer ChannelChange Enter Fail : " + CHAT_TYPE.CT_NORMAL);

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
                    if (!await EnterUserChannel(type))
                    {
                        Logger.WriteLog("ChatPlayer Normal EnterUserChannel Fail : " + UserData.UserUID);
                        return false;
                    }
                    break;

                case CHAT_TYPE.CT_GUILD:
                    GuildChannel = channelNum;
                    ch = GetGuildChannel();
                    if (!await EnterUserChannel(type))
                    {
                        Logger.WriteLog("ChatPlayer Guild EnterUserChannel Fail : " + UserData.UserUID);
                        return false;
                    }
                    break;
                case CHAT_TYPE.CT_SYSTEM:
                    ch = Constance.SYSTEM;
                    break;
                case CHAT_TYPE.CT_GM_NOTICE:
                    ch = Constance.GM_NOTICE;
                    break;
            }

            Logger.WriteLog("EnterChannel : " + ch + "-" + UserData.UserUID);
            var result = await RedisManager.Instance.SubscribeAction(ch, UserData, action);
            if (!result)
                Logger.WriteLog("ChatPlayer EnterChannel Subscribe Fail : " + ch);

            return true;
        }

        public async Task<bool> LeaveChannel(CHAT_TYPE type)
        {
            Logger.WriteLog("LeaveChannel :" + type);
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

            if (!RedisManager.Instance.UnSubscribe(channel, UserData.ID).Result)
                return false;

            //await UserStateChannel(CHAT_ENTER_STATE.CT_ENTER, type, channel);
            await LeaveUserChannel(type);
            return true;
        }

        public async Task<bool> LeaderChange(req_ChatLeaderChange message)
        {
            if (message == null)
            {
                Logger.WriteLog("ChatPlayer LeaderChange Message Null");
                return false;
            }

            await Task.Run(() => UserData.CharacterID = message.LeaderCharacterID);

            return true;
        }

        public void GachaNotice(req_ChatGachaNotice message)
        {
            if (message == null)
            {
                Logger.WriteLog("ChatPlayer GachaNotice Message Null");
                return;
            }

        }


        public void LeaveAllChannel()
        {
            //channelDict.AsParallel().ForAll(entry => Task.Run(() => RedisManager.Instance.UnSubscribe(entry.Key.ToString() + entry.Value, this.SessionID)));

            RedisManager.Instance.LeaveUser(UserData.ID);
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
                    await RedisManager.Instance.ForcePublish(GetNormalChannel(), EncodingJson.Serialize(enterUser));
                    break;
                case CHAT_TYPE.CT_GUILD:
                    await RedisManager.Instance.ForcePublish(GetGuildChannel(), EncodingJson.Serialize(enterUser));
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
                    await RedisManager.Instance.ForcePublish(GetNormalChannel(), EncodingJson.Serialize(leaveUser));
                    break;

                case CHAT_TYPE.CT_GUILD:
                    await RedisManager.Instance.ForcePublish(GetNormalChannel(), EncodingJson.Serialize(leaveUser));
                    break;

                default:
                    return;
            }

        }

        public async Task<bool> ReceiveEnd()
        {
            if (!RedisManager.Instance.UnSubscribe(GetNormalChannel(), UserData.ID).Result)
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
