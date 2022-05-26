using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    // 채널서버에 접속한 개별 플레이어 관리
    class ChatPlayer
    {
        public string SessionID { get; set; }
        public string UID { get; set;}
        public string Name { get; set; }
        public string GuildID { get; set; }                       // 
        public Dictionary<string, string> Channels = null;        // 현재 참여중인 채널 <채널이름, 채널번호>
        
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

        public bool SendMessage(Message message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer PushMessage Message Null");
                return false;
            }
            
            Task.Run(() => RedisManager.Instance.Publish(message));
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

        public bool EnterChannel(string channel, string sessionId)
        {
            if (string.IsNullOrEmpty(channel))
            {
                Console.WriteLine("ChatPlayer EnterChannel Error");
                return false;
            }

            Task.Run(() => RedisManager.Instance.Subscribe(channel, sessionId));

            return true;
        }

        public bool ChangeChannel(string channel)
        {
            if (string.IsNullOrEmpty(channel))
            {
                Console.WriteLine("ChatPlayer ChangeChannel Error");
                return false;
            }

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

        public bool ReportMessage()
        {
            //...
            return true;
        }

        
    }
}
