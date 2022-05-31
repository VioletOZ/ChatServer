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
        
        // 1:1 대화등의 메시지 저장용
        public Dictionary<string, BlockingCollection<string>> WhisperMessage { get; set; }
        

        public ChatPlayer(string sessionId, string uid, string name)
        {
            this.SessionID = sessionId;
            this.UID = uid;
            this.Name = name;
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
            }

            string ch;
            switch (message.ChatType)
            {
                case CHAT_TYPE.CT_NORMAL:
                     ch = Constance.NORMAL + NormalChannel;
                    _ = Task.Run(() => RedisManager.Instance.Publish(ch, message));
                    break;

                case CHAT_TYPE.CT_GUILD:
                    ch = Constance.GUILD + GuildChannel;
                    _ = Task.Run(() => RedisManager.Instance.Publish(ch, message));
                    break;

                case CHAT_TYPE.CT_SYSTEM:
                    ch = Constance.SYSTEM;
                    _ =  Task.Run(() => RedisManager .Instance.Publish(ch, message));
                    break;

                case CHAT_TYPE.CT_GM_NOTICE:
                    ch = Constance.GM_NOTICE;
                    _ = Task.Run(() => RedisManager .Instance.Publish(ch, message));
                    break;
            }
        }

        public async void RecvMessage(res_ChatMessage message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer RecvMessage Message Null");
            }

            await Task.Run(() => RedisManager.Instance.UnSubscribe(message.ChannelID.ToString(), null));
            //string temp = Channels[channel].Dequeue();
        }

        public  async void Reconnect(req_ChatReConnect message)
        {
            
        }
        
        public async void LogOut(req_ChatLogout message)
        {
            
        }

        public async void ChannelInfo(req_ChatInfo message)
        {
            
        }

        public async void GuildChatLog(req_ChatGuildLog message)
        {

        }

        // 채널변경은 일반 채널밖에 되지않음.
        public async Task<bool> ChangeChannel(req_ChatChangeChannel message, Action<RedisChannel, RedisValue> action)
        {
            // 변경 전 채널
            int beforeChannel = NormalChannel;
            // 기존 채널 정보 변경후에
            channelDict[CHAT_TYPE.CT_NORMAL] = message.ChannelID;
            Console.WriteLine("Change Channel : " + channelDict[CHAT_TYPE.CT_NORMAL]);

            // 구독중인 채널 삭제하고
            string ch = Constance.NORMAL + beforeChannel.ToString();
            await RedisManager.Instance.UnSubscribe(ch, SessionID);
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
            await RedisManager.Instance.SubscribeAction(ch, action);

            //string message = Name + " 님이 입장하셨습니다.";
            //RedisManager.Instance.ForcePublish(ch, message);

            return true;
        }

        public async void LeaveChannel(req_ChatLeaveChannel message)
        {
            
        }

        public async void LeaderChange(req_ChatLeaderChange argData)
        {
            
        }

        public async void GaChaNotice(req_ChatGachaNotice argData)
        {
            
        }


        public bool LeaveAllChannel()
        {
            string ch = Constance.NORMAL + NormalChannel.ToString(); ;
            //channelDict.AsParallel().ForAll(entry => Task.Run(() => RedisManager.Instance.UnSubscribe(entry.Key.ToString() + entry.Value, this.SessionID)));
            _ = RedisManager.Instance.UnSubscribe(ch, SessionID);
            return true;
        }

        public bool ReportMessage()
        {
            //...
            return true;
        }

        
    }
}
