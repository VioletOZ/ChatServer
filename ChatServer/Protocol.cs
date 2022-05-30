﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace ChatServer
{
    // 로그인
    public class req_ChatLogin
    {
        // 로그인시에는 Request 헤더에 
        // SessionID, UserUID, UserName, GuildID, FavoriteCharacterID 를 key로 요청
    }

    public class res_ChatLogin
    {
        public RETURN_CODE ReturnCode { get; set; }
        public int ChannelID { get; set; }
        public int GuildChannelID { get; set; }     // 로그인시에 Guild있을경우 자동채널입장 채널이 2개뿐이니 ChatType없이 모두 입장
    }

    public class req_ChatReConnect
    {
        public string SessionID { get; set; }
        public string UserUID { get; set; }
    }

    public class res_ChatReConnect
    {
        public RETURN_CODE ReturnCode { get; set; }        
    }

    // 로그아웃 
    public class req_ChatLogout
    {
        public string SessionID { get; set; }
        public string UserUID { get; set; }
    }

    public class res_ChatLogout
    {
        public RETURN_CODE ReturnCode { get; set; }
    }

    // 채널의 유저 정보 요청
    public class req_ChatInfo
    {
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
    }

    public class res_ChatInfo
    {
        public RETURN_CODE ReturnCode { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
        public List<ChatUserData> ChannelUserDataList;
    }

    // 길드 채팅로그 요청
    public class req_ChatGuildLog
    {
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
    }

    public class res_ChatGuildLog
    {
        public RETURN_CODE ReturnCode { get; set; }
        public List<ChatLogData> GuildLogDataList;

    }

    // 채널 변경
    public class req_ChatChange
    {
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
    }

    public class res_ChatChange
    {
        public RETURN_CODE ReturnCode { get; set; }
        public int ChannelID { get; set; }
        public List<ChatUserData> ChannelUserDataList;
    }

    // 채널 입장 - 채널 변경과 중복되지만 호출 구분을위해서 따로 처리
    public class req_ChatEnterChannel
    {
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
    }
    public class res_ChatEnterChannel
    {
        public RETURN_CODE ReturnCode { get; set; }
        public int ChannelID { get; set; }
        public List<ChatUserData> ChannelUserDataList;
    }

    // 채널 나가기 (길드탈퇴시)
    public class req_ChatLeaveChannel
    {
        // 길드마스터일경우나 탈퇴시 후처리가 필요할 경우 추가 데이터 요청 해야함
    }
    public class res_ChatLeaveChannel
    {
        public RETURN_CODE ReturnCode { get; set; }
        // 길드마스터일경우나 탈퇴시 후처리가 필요할 경우 추가 데이터 전송 해야함
    }

    // CHAT_COMMAND 로 채널입장(변경)
    // CHAT_TYPE 으로 채팅 or 이모티콘 or 아이템
    public class req_ChatMessage
    {
        public CHAT_COMMAND Command { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
        public ChatLogData LogData;
    }
    public class res_ChatMessage
    {
        public RETURN_CODE ReturnCode { get; set; }
        public CHAT_TYPE ChatType { get; set; }
        public int ChannelID { get; set; }
        public ChatLogData LogData;
    }

    // 가챠 노티 응답받기
    public class res_ChatGachaNotice
    {
        public RETURN_CODE ReturnCode { get; set; }
        public string UserName { get; set; }
        public int ItemID { get; set; }
        public int CharID { get; set; }
    }
}