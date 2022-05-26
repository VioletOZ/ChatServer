using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    public enum ERROR_CODE
    {
        SUCCESS = 0,
        ERROR = 1,
        NULLDATA = 2
    }
    public enum CHAT_TYPE
    {
        NORMAL = 0,
        WHISPER,
        GUILD,
        SYSTEM,
        SERVER
    }
    public enum CONNECT_STATE
    {
        CONNECTING = 0,
        OPEN = 1,
        CLOSING = 2,
        CLOSE = 3,
        ERROR = 99
    }

    public enum CHAT_COMMAND
    {
        CHAT = 0,                                               // 채팅
        CHANGE_CHANNEL,                                         // 채널 변경
        ENTER_GUILD_CHANNEL,                                    // 길드 채널 입장
        REPORT                                                  // 신고
    }

    static class Constance
    {
        public const string ADDRESS = "127.0.0.1";
        public const int PORT = 9000;
        public const int CHANNEL_PLAYER_MAX = 50;               // 채널당 최대인원
        public const int CHANNEL_MAX = 9999;                    // 최대 채널갯수

    }
    
}
