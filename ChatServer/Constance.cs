using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    public enum RETURN_CODE
    {
        RC_OK = 0,                                              // 성공
        RC_FAIL = 1,                                            // 실패
        RC_CHAT_LOGIN_FAIL = 16                                 // 챗서버 접속 실패
    }

    public enum CHAT_TYPE
    {
        CT_INVALID = 0,                                         // 
        CT_NORMAL,                                              // 일반채널
        CT_GUILD,                                               // 길드 채팅
        CT_WHISPER,                                             // 귓속말
        CT_SYSTEM,                                              // 시스템
        CT_GM_NOTICE,                                           // GM 알림

        CT_MAX
    }
    
    public enum CHAT_COMMAND
    {
        CT_CHAT = 0,                                               // 채팅
        CT_CHANGE_CHANNEL,                                         // 채널 변경
        CT_ENTER_GUILD_CHANNEL,                                    // 길드 채널 입장
        CT_REPORT                                                  // 신고
    }

    public enum CONNECT_STATE
    {
        CT_CONNECTING = 0,
        CT_OPEN = 1,
        CT_CLOSING = 2,
        CT_CLOSE = 3,
        CT_ERROR = 99
    }

    static class Constance
    {
        public const string ADDRESS = "127.0.0.1";
        public const int PORT = 9000;
        public const int CHANNEL_PLAYER_MAX = 50;               // 채널당 최대인원 임시값
        public const int CHANNEL_MAX = 9999;                    // 최대 채널갯수 임시값

        public const string NORMAL = "NORMAL";
        public const string GUILD = "GUILD";
        public const string SYSTEM = "SYSTEM";
        public const string GM_NOTICE = "GM_NOTICE";
    }
    
}
