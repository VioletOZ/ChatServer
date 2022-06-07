using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace ChatServer
{
    internal class SessionState
    {
        public string ServerSessionID { get; set; }
        public ISubscriber subscriber;
        public IDatabase db;
    }
}
