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

        public bool SendMessage(Message message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer PushMessage Message Null");
                return false;
            }

            string ch;
            switch (message.Type)
            {
                case CHAT_TYPE.NORMAL:
                     ch = Constance.NORMAL + NormalChannel;
                    Task.Run(() => RedisManager.Instance.Publish(ch, message));
                    break;

                case CHAT_TYPE.GUILD:
                    ch = Constance.GUILD + GuildChannel;
                    Task.Run(() => RedisManager.Instance.Publish(ch, message));
                    break;

                case CHAT_TYPE.SYSTEM:
                    ch = Constance.SYSTEM;
                    Task.Run(() => RedisManager.Instance.Publish(ch, message));
                    break;

                case CHAT_TYPE.SERVER:
                    ch = Constance.SERVER;
                    Task.Run(() => RedisManager.Instance.Publish(ch, message));
                    break;
            }
            
            return true;
        }

        public bool RecvMessage(Message message)
        {
            if (message == null)
            {
                Console.WriteLine("ChatPlayer RecvMessage Message Null");
                return false;
            }

            Task.Run(() => RedisManager.Instance.UnSubscribe(message.Channel.ToString(), null));
            //string temp = Channels[channel].Dequeue();
            return true;
        }

        public async Task<bool> EnterChannel(CHAT_TYPE channel, int channelNum, Action<RedisChannel, RedisValue> action)
        {
            //Task.Run(() => RedisManager.Instance.Subscribe(channel, sessionId));
            if (!channelDict.ContainsKey(channel))
                channelDict.Add(channel, channelNum);

            string ch = "";
            switch (channel)
            {
                case CHAT_TYPE.NORMAL:
                    NormalChannel = channelNum;
                    ch = Constance.NORMAL + channelNum.ToString();
                    break;

                case CHAT_TYPE.GUILD:
                    ch = Constance.GUILD + channelNum.ToString();
                    break;
                case CHAT_TYPE.SYSTEM:
                    ch = Constance.SYSTEM;
                    break;
                case CHAT_TYPE.SERVER:
                    ch = Constance.SERVER;
                    break;
            }
            

            Console.WriteLine("EnterChannel : " + ch);
            await RedisManager.Instance.SubscribeAction(ch, action);

            string message = Name + " 님이 입장하셨습니다.";
            _ = RedisManager.Instance.ForcePublish(ch, message);

            return true;
        }

        // 채널변경은 일반 채널밖에 되지않음.
        public async Task<bool> ChangeChannel(int channelNum, Action<RedisChannel, RedisValue> action)
        {
            try
            {
                // 변경 전 채널
                int beforeChannel = NormalChannel;
                // 기존 채널 정보 변경후에
                channelDict[CHAT_TYPE.NORMAL] = channelNum;
                Console.WriteLine("Change Channel : " + channelDict[CHAT_TYPE.NORMAL]);

                // 구독중인 채널 삭제하고
                string ch = Constance.NORMAL + beforeChannel.ToString();
                await RedisManager.Instance.UnSubscribe(ch, SessionID);
                NormalChannel = channelNum;

                // 다시 구독요청
                Console.WriteLine("ChangeChannel : " + CHAT_TYPE.NORMAL.ToString() + channelNum);
                if (!await EnterChannel(CHAT_TYPE.NORMAL, channelNum, action))
                    Console.WriteLine("ChatPlayer ChannelChange>Enter Fail");

                string message = Name + " 님이 나가셨습니다.";
                _ = RedisManager.Instance.ForcePublish(beforeChannel.ToString(), message);

            }
            catch (Exception e)
            {
                Console.WriteLine("ChatPlayer ChangeChannel Exception : " + e.Message);
                return false;
            }
            
            return true;
        }

        public bool EnterGuildChannel(CHAT_TYPE channel)
        {
            
            return true;
        }

        public bool LeaveGuildChannel(CHAT_TYPE channel)
        {
            
            return true;
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
