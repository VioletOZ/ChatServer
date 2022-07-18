using System;
using System.Net;
using System.Net.Sockets;
using WebSocketSharp;
using WebSocketSharp.Server;
using StackExchange.Redis;
using StackExchange.Redis.MultiplexerPool;

using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //string ENV_CHAT_SERVER_PORT = Environment.GetEnvironmentVariable("ENV_CHAT_SERVER_PORT");
            //Logger.WriteLog("Server Port : " + ENV_CHAT_SERVER_PORT);
            Logger.WriteLog("Server Port : " + Constance.ENV_CHAT_SERVER_PORT);
            Logger.WriteLog("ServerLog Path : " + Constance.ENV_CHAT_SERVER_LOG_PATH);
            // 웹소켓 초기화
            WebSocketServer webSocketServer = null;
            if (webSocketServer != null)
                return;

            webSocketServer = new WebSocketServer(Convert.ToInt32(Constance.ENV_CHAT_SERVER_PORT));
            webSocketServer.AddWebSocketService<Chat>("/Chat");

            //서버시작
            webSocketServer.Start();

            // 서버 주소 표기용
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            string ipAddr = string.Empty;
            for (int i = 0; i < host.AddressList.Length; i++)
            {
                if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddr = host.AddressList[i].ToString();
                    break;
                }
            }

            Logger.WriteLog("ServerStart - " + ipAddr + ":" + webSocketServer.Port);            
            Logger.WriteLog("ESC Exit");
            while (true)
            {
                //if (Console.KeyAvailable)
                //{
                //    var keyInfo = Console.ReadKey();
                //    if (keyInfo.Key == ConsoleKey.Escape)
                //    {
                //        //queue.CompleteAdding();
                //        Console.ReadLine();
                //        break;
                //    }
                //}
            }
        }
    }
}
