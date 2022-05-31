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
        public string UID { get; set;}
        public string Name { get; set; }
        public string GuildID { get; set; }                       // 
        public Dictionary<CHAT_TYPE, int> channelDict = new Dictionary<CHAT_TYPE, int>();        // 현재 참여중인 채널 <채널이름, 채널번호>

        public int NormalChannel { get; set; }              // 쓰기편하려고.. 
        public int GuildChannel { get; set; }

        public ChatUserData userData = new ChatUserData();
        // 1:1 대화등의 메시지 저장용
        public Dictionary<string, BlockingCollection<string>> WhisperMessage { get; set; }
        

        public ChatPlayer(string sessionId, string uid, string name, int charId)
        {
            this.SessionID = sessionId;
            this.UID = uid;
            this.Name = name;

            this.userData.UserUID = uid;
            this.userData.UserName = name;
            this.userData.CharacterID = charId;
        }

        public void SetPlayer(string sid, string uid, string name, string gid)
        {
            SessionID = sid;
            UID = uid;
            Name = name;
            GuildID = gid;
        }

        public async Task<bool> AuthVerify()
        {
            var result = await RedisManager.Instance.AuthVerify(SessionID);
            if (!result)
                return false;
            return true;
        }

        public async void SendMessage(req_ChatMessage message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer PushMessage Message Null");
                return;
            }

            switch (message.ChatType)
            {
                case CHAT_TYPE.CT_NORMAL:
                    await Task.Run(() => RedisManager.Instance.Publish(GetNormalChannel(), message));
                    break;

                case CHAT_TYPE.CT_GUILD:
                    await Task.Run(() => RedisManager.Instance.Publish(GetGuildChannel(), message));
                    break;

                case CHAT_TYPE.CT_SYSTEM:
                    await Task.Run(() => RedisManager .Instance.Publish(Constance.SYSTEM, message));
                    break;

                case CHAT_TYPE.CT_GM_NOTICE:
                    await Task.Run(() => RedisManager .Instance.Publish(Constance.GM_NOTICE, message));
                    break;
            }
        }

        public async void RecvMessage(res_ChatMessage message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer RecvMessage Message Null");
                return;
            }

            await Task.Run(() => RedisManager.Instance.UnSubscribe(message.ChannelID.ToString(), null));
            //string temp = Channels[channel].Dequeue();
        }

        public  void Reconnect(req_ChatReConnect message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer Reconnect Message Null");
                return;
            }
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

        public void GuildChatLog(req_ChatGuildLog message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer RecvMessage Message Null");
                return;
            }
        }

        // 채널변경은 일반 채널밖에 되지않음.
        public async Task<bool> ChangeChannel(req_ChatChangeChannel message, ChatUserData userData, Action<RedisChannel, RedisValue> action)
        {
            // 변경 전 채널
            int beforeChannel = NormalChannel;
            // 기존 채널 정보 변경후에
            channelDict[CHAT_TYPE.CT_NORMAL] = message.ChannelID;
            Console.WriteLine("Change Channel : " + channelDict[CHAT_TYPE.CT_NORMAL]);

            // 구독중인 채널 삭제하고
            string ch = Constance.NORMAL + beforeChannel.ToString();
            await RedisManager.Instance.UnSubscribe(ch, userData);
            NormalChannel = message.ChannelID;

            // 다시 구독요청
            Console.WriteLine("ChangeChannel : " + CHAT_TYPE.CT_NORMAL.ToString() + message.ChannelID);
            if (!await EnterChannel(CHAT_TYPE.CT_NORMAL, message.ChannelID, action))
                Console.WriteLine("ChatPlayer ChannelChange>Enter Fail");

            string words = Name + " 님이 나가셨습니다.";
            RedisManager.Instance.ForcePublish(beforeChannel.ToString(), words);

            return true;
        }

        public async Task<bool> EnterChannel(CHAT_TYPE chatType, int channelNum, Action<RedisChannel, RedisValue> action)
        {
            //Task.Run(() => RedisManager.Instance.Subscribe(channel, sessionId));
            if (!channelDict.ContainsKey(chatType))
                channelDict.Add(chatType, channelNum);

            string ch = "";
            switch (chatType)
            {
                case CHAT_TYPE.CT_NORMAL:
                    NormalChannel = channelNum;
                    ch = Constance.NORMAL + channelNum.ToString();
                    break;

                case CHAT_TYPE.CT_GUILD:
                    ch = Constance.GUILD + channelNum.ToString();
                    break;
                case CHAT_TYPE.CT_SYSTEM:
                    ch = Constance.SYSTEM;
                    break;
                case CHAT_TYPE.CT_GM_NOTICE:
                    ch = Constance.GM_NOTICE;
                    break;
            }

            Console.WriteLine("EnterChannel : " + ch);
            await RedisManager.Instance.SubscribeAction(ch, userData, action);

            //string message = Name + " 님이 입장하셨습니다.";
            //RedisManager.Instance.ForcePublish(ch, message);

            return true;
        }

        public async Task<bool> LeaveChannel(req_ChatLeaveChannel message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer LeaveCHannel Message Null");
                return false;
            }

            await RedisManager.Instance.UnSubscribe(GetGuildChannel(), userData);

            return true;
        }

        public async Task<bool> LeaderChange(req_ChatLeaderChange message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer LeaderChange Message Null");
                return false;
            }

            await Task.Run(() => userData.CharacterID = message.LeaderCharacterID);

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


        public async void LeaveAllChannel()
        {
            string ch = Constance.NORMAL + NormalChannel.ToString();
            await RedisManager.Instance.UnSubscribe(ch, userData);
            //channelDict.AsParallel().ForAll(entry => Task.Run(() => RedisManager.Instance.UnSubscribe(entry.Key.ToString() + entry.Value, this.SessionID)));

            ch = Constance.GUILD + GuildChannel.ToString();
            await RedisManager.Instance.UnSubscribe(ch, userData);
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
