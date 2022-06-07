using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace ChatServer
{
    public class req_Command
    {
        public CHAT_COMMAND Command { get; set; }
    }

    // 로그인
    public class req_ChatLogin
    {
        // 로그인시에는 Request 헤더에 
        // SessionID, UserUID, UserName, GuildID, FavoriteCharacterID 를 key로 요청
    }

    public class res_ChatLogin
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
        public int ChannelID { get; set; }
        public int GuildChannelID { get; set; }     // 로그인시에 Guild있을경우 자동채널입장 채널이 2개뿐이니 ChatType없이 모두 입장
    }

    public class req_ChatReConnect
    {
        public CHAT_COMMAND Command { get; set; }
        public string SessionID { get; set; }
        public long UserUID { get; set; }
    }

    public class res_ChatReConnect
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }        
    }

    // 로그아웃 
    public class req_ChatLogout
    {
        public CHAT_COMMAND Command { get; set; }
        public string SessionID { get; set; }
        public long UserUID { get; set; }
    }

    public class res_ChatLogout
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
    }

    // 채널의 유저 정보 요청
    public class req_ChatInfo
    {
        public CHAT_COMMAND Command { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
    }

    public class res_ChatInfo
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
        public List<ChatUserData> ChannelUserDataList { get; set; }
    }

    // 길드 채팅로그 요청
    public class req_ChatGuildLog
    {
        public CHAT_COMMAND Command { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
    }

    public class res_ChatGuildLog
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
        public List<ChatGuildLogData> GuildLogDataList { get; set; }

    }

    // 채널 변경
    public class req_ChatChangeChannel
    {
        public CHAT_COMMAND Command { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
    }

    public class res_ChatChangeChannel
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
        public int ChannelID { get; set; }
        public List<ChatUserData> ChannelUserDataList { get; set; }
    }

    // 채널 입장 - 채널 변경과 중복되지만 호출 구분을위해서 따로 처리
    public class req_ChatEnterChannel
    {
        public CHAT_COMMAND Command { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
    }
    public class res_ChatEnterChannel
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
        public List<ChatUserData> ChannelUserDataList { get; set; }
    }

    // 채널 나가기
    public class req_ChatLeaveChannel
    {
        public CHAT_COMMAND Command { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        // 길드마스터일경우나 탈퇴시 후처리가 필요할 경우 추가 데이터 요청 해야함
    }
    public class res_ChatLeaveChannel
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        // 길드마스터일경우나 탈퇴시 후처리가 필요할 경우 추가 데이터 전송 해야함
    }

    // CHAT_COMMAND 로 채널입장(변경)
    // CHAT_TYPE 으로 채팅 or 이모티콘 or 아이템
    public class req_ChatMessage
    {
        public CHAT_COMMAND Command { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
        public ChatLogData LogData { get; set; }
    }

    public class res_ChatMessage
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
        public ChatLogData LogData { get; set; }
    }

    // 가챠 노티
    public class req_ChatGachaNotice
    {
        public CHAT_COMMAND Command { get; set; }
        public string UserName { get; set; }
        public List<int> ItemIDList { get; set; }
        public List<int> CharIDList { get; set; }
    }

    public class res_ChatGachaNotice
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
        public string UserName { get; set; }
        public List<int> ItemIDList { get; set; }
        public List<int> CharIDList { get; set; }
    }

    public class req_ChatLeaderChange
    {
        public CHAT_COMMAND Command { get; set; }
        public int LeaderCharacterID { get; set; }
    }

    public class res_ChatLeaderChange
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
    }

    // 채널 입장시 내 정보 알림
    public class res_ChatEnterUser
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public ChatUserData UserData { get; set; }
    }

    // 채널 퇴장시 내정보 알림
    public class res_ChatLeaveUser
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public long UserUID { get; set; }
    }

    // 일반 채널 로그 안받기
    public class req_ChatReceiveEnd
    {
        public CHAT_COMMAND Command { get; set; }
    }

    public class res_ChatReceiveEnd
    {
        public CHAT_COMMAND Command { get; set; }
        public RETURN_CODE ReturnCode { get; set; }
    }
}
