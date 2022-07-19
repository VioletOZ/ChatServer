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
        RC_CHAT_LOGIN_FAIL = 16,                                // 챗서버 접속 실패
        RC_CHAT_DUPLICATE_CHANNEL = 100                         // 중복된 채널 접속요청
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
        CT_LOGIN = 0,                                               // 로그인
        CT_RECONNECT = 1,                                           // 재접속
        CT_LOGOUT = 2,                                              // 로그아웃
        CT_INFO = 3,                                                // 채팅채널에 유저정보
        CT_GUILD_LOG = 4,                                           // 길드 채팅로그
        CT_LEADER_CHANGE = 5,                                       // 대표 캐릭터 변경
        CT_NORMAL_LOG = 6,                                          // 일반 채팅 로그


        CT_MESSAGE = 10,                                            // 채팅
        CT_CHANNEL_CHANGE = 11,                                      // 채널 변경
        CT_CHANNEL_ENTER = 12,                                     // 채널 입장 (길드만 일반채널은 변경만가능)
        CT_CHANNEL_LEAVE = 13,                                      // 채널 나가기(길드탈퇴시)
        CT_CHANNEL_ENTER_USER = 14,                                 // 채널 유저 상태
        CT_CHANNEL_LEAVE_USER = 15,                                 // 채널 유저 상태
        CT_CHANNEL_RECEIVE_END = 16,                                // 로그 안받기

        CT_NOTICE_GACHA = 100,                                      // 가챠 노티 
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
        // Game Server Redis
        public static readonly string ENV_GAME_SERVER_REDIS_ADDR = Environment.GetEnvironmentVariable("ENV_GAME_SERVER_REDIS_ADDR");// == null ? "192.168.0.211" : Environment.GetEnvironmentVariable("ENV_GAME_SERVER_REDIS_ADDR");
        public static readonly string ENV_GAME_SERVER_REDIS_PORT = Environment.GetEnvironmentVariable("ENV_GAME_SERVER_REDIS_PORT");// == null ? "6379" : Environment.GetEnvironmentVariable("ENV_GAME_SERVER_REDIS_PORT");

        // Chat Server Redis
        public static readonly string ENV_CHAT_SERVER_REDIS_ADDR = Environment.GetEnvironmentVariable("ENV_CHAT_SERVER_REDIS_ADDR");// == null ? "127.0.0.1" : Environment.GetEnvironmentVariable("ENV_CHAT_SERVER_REDIS_ADDR");
        public static readonly string ENV_CHAT_SERVER_REDIS_PORT = Environment.GetEnvironmentVariable("ENV_CHAT_SERVER_REDIS_PORT");// == null ? "6379" : Environment.GetEnvironmentVariable("ENV_CHAT_SERVER_REDIS_PORT");

        // Chat Server
        public static readonly string ENV_CHAT_SERVER_PORT = Environment.GetEnvironmentVariable("ENV_CHAT_SERVER_PORT");// == null ? "9000" : Environment.GetEnvironmentVariable("ENV_CHAT_SERVER_LOG_PATH");
        public static readonly string ENV_CHAT_SERVER_LOG_PATH = Environment.GetEnvironmentVariable("ENV_CHAT_SERVER_LOG_PATH") == null ? "/server/log" : Environment.GetEnvironmentVariable("ENV_CHAT_SERVER_LOG_PATH");

        public const int CHANNEL_PLAYER_MAX = 50;               // Max Player 
        public const int CHANNEL_MAX = 9999;                    // Max Channel
        
        public const string NORMAL = "NORMAL";
        public const string GUILD = "GUILD";
        public const string SYSTEM = "SYSTEM";
        public const string GM_NOTICE = "GM_NOTICE";
        public const string LOG = "LOG";
        public const int PAGE_SIZE = 250;
        public const int LOG_COUNT = 100;
        public const int POOL_SIZE = 50000;

        public static int POSSIBLE_CHANNEL_NUMBER = 1;
    }

}
