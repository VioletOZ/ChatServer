using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public Dictionary<string, string> channelDict = new Dictionary<string, string>();        // 현재 참여중인 채널 <채널이름, 채널번호>

        private string NormalChannel { get; set; }              // 쓰기편하려고.. 
        
        // 1:1 대화등의 메시지 저장용
        public Dictionary<string, BlockingCollection<string>> WhisperMessage { get; set; }
        

        public ChatPlayer(string sessionId, string uid, string name, string guildId)
        {
            this.SessionID = sessionId;
            this.UID = uid;
            this.Name = name;
            this.GuildID = guildId;
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

        public bool SendMessage(Message message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer PushMessage Message Null");
                return false;
            }
            
            Task.Run(() => RedisManager.Instance.Publish(NormalChannel, message));
            return true;
        }

        public bool RecvMessage(Message message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer RecvMessage Message Null");
                return false;
            }

            Task.Run(() => RedisManager.Instance.UnSubscribe(message.Channel, null));
            //string temp = Channels[channel].Dequeue();
            return true;
        }

        public bool EnterChannel(string channel, string channelNum, Action<RedisChannel, RedisValue> action)
        {
            if (string.IsNullOrEmpty(channel))
            {
                Console.WriteLine("ChatPlayer EnterChannel Error");
                return false;
            }

            //Task.Run(() => RedisManager.Instance.Subscribe(channel, sessionId));
            if (!channelDict.ContainsKey(channel))
                channelDict.Add(channel, channelNum);

            NormalChannel = channel + channelNum; // 채널 + 채널번호

            Console.WriteLine("EnterChannel : " + NormalChannel);
            Task.Run(() => RedisManager.Instance.SubscribeAction(NormalChannel, action));

            return true;
        }

        // 채널변경은 일반 채널밖에 되지않음.
        public async Task<bool> ChangeChannel(string channel, Action<RedisChannel, RedisValue> action)
        {
            if (string.IsNullOrEmpty(channel))
            {
                Console.WriteLine("ChatPlayer ChangeChannel Error");
                return false;
            }

            // 기존 채널 정보 변경후에
            channelDict[CHAT_TYPE.NORMAL.ToString()] = channel;
            Console.WriteLine("Change Channel : " + channelDict[CHAT_TYPE.NORMAL.ToString()]);

            // 구독중인 채널 삭제하고
            await RedisManager.Instance.UnSubscribe(NormalChannel, SessionID);
            NormalChannel = CHAT_TYPE.NORMAL.ToString() + channel;

            // 다시 구독요청
            Console.WriteLine("ChangeChannel : " + CHAT_TYPE.NORMAL.ToString() + channel);
            if (!EnterChannel(CHAT_TYPE.NORMAL.ToString(), channel, action))
                Console.WriteLine("ChatPlayer ChannelChange>Enter Fail");

            return true;
        }

        public bool EnterGuildChannel(string channel)
        {
            if (string.IsNullOrEmpty(channel))
            {
                Console.WriteLine("ChatPlayer EnterGuildChannel Error");
                return false;
            }

            return true;
        }

        public bool LeaveGuildChannel(string channel)
        {
            if (string.IsNullOrEmpty(channel))
            {
                Console.WriteLine("ChatPlayer LeaveGuildChannle Error");
                return false;
            }

            return true;
        }

        public bool LeaveAllChannel()
        {
            channelDict.AsParallel().ForAll(entry => Task.Run(() => RedisManager.Instance.UnSubscribe(entry.Key + entry.Value, this.SessionID)));
            return true;
        }

        public bool ReportMessage()
        {
            //...
            return true;
        }

        
    }
}
