using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    public enum ERROR_CODE
    {
        SUCCESS = 0,                                            // 성공
        ERROR = 1,                                              // 실패
    }

    public enum CHAT_TYPE
    {
        NORMAL = 0,                                             // 일반채널
        GUILD,                                                  // 길드
        SYSTEM,                                                 // 시스템
        SERVER                                                  // 서버
    }
    
    public enum CHAT_COMMAND
    {
        CHAT = 0,                                               // 채팅
        CHANGE_CHANNEL,                                         // 채널 변경
        ENTER_GUILD_CHANNEL,                                    // 길드 채널 입장
        REPORT                                                  // 신고
    }

    public enum CONNECT_STATE
    {
        CONNECTING = 0,
        OPEN = 1,
        CLOSING = 2,
        CLOSE = 3,
        ERROR = 99
    }

    static class Constance
    {
        public const string ADDRESS = "127.0.0.1";
        public const int PORT = 9000;
        public const int CHANNEL_PLAYER_MAX = 50;               // 채널당 최대인원
        public const int CHANNEL_MAX = 9999;                    // 최대 채널갯수

        public const string NORMAL = "NORMAL";
        public const string GUILD = "GUILD";
        public const string SYSTEM = "SYSTEM";
        public const string SERVER = "SERVER";
    }
    
}
