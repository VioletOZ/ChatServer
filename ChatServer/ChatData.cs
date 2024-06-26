﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace ChatServer
{
    public class ChatUserData
    {
        public string ID { get; set; }
        public long UserUID { get; set; }
        public string UserName { get; set; }
        public int CharacterID { get; set; }
    }

    public class ChatUserDataEx
    {
        public ChatUserData UserData { get; set; }
        public Action<RedisChannel, RedisValue> Handler { get; set; }
    }

    public class ChatLogData
    {
        public long UserUID { get; set; }
        public string UserName { get; set; }
        public int LeaderCharacterID { get; set; }
        public int EmoticonNum { get; set; }
        public int CharacterID { get; set; }
        public int ItemID { get; set; }
        public string Text { get; set; }
    }

    public class ChatGuildLogData
    {
        public DateTime Time { get; set; }
        public long UserUID { get; set; }
        public string UserName { get; set; }
        public int LeaderCharacterID { get; set; }
        public int EmoticonNum { get; set; }
        public int CharacterID { get; set; }
        public int ItemID { get; set; }
        public string Text { get; set; }
    }
}
