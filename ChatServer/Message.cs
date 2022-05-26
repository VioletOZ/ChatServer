﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace ChatServer
{
    public class Message
    {
        public CHAT_COMMAND Command { get; set;}
        public CHAT_TYPE Type { get; set; }
        public string SessionID { get; set; }
        public string UID { get; set; }
        public string Name { get; set; }
        public string Channel { get; set; }
        public string Text { get; set; }
        public string TargetUID { get; set; }
        
    }
}
