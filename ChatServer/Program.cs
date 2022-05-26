using System;
using System.Net;
using System.Net.Sockets;
using WebSocketSharp;
using WebSocketSharp.Server;
using StackExchange.Redis;

using System.Threading;
using System.Threading.Tasks;


namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // 웹소켓 초기화
            WebSocketServer webSocketServer = null;

            if (webSocketServer != null)
                return;

            //webSocketServer = new WebSocketServer(9000);
            webSocketServer = new WebSocketServer(Constance.PORT);
            webSocketServer.AddWebSocketService<EventHandler>("/Chat");

            //서버시작
            webSocketServer.Start();

            // 레디스 기본 채널 구독
            Task.Run(() => RedisManager.Instance.Subscribe(CHAT_TYPE.SYSTEM.ToString()));
            Task.Run(() => RedisManager.Instance.Subscribe(CHAT_TYPE.SERVER.ToString()));

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

            Console.WriteLine("ServerStart - " + ipAddr + ":" + webSocketServer.Port);            
            Console.WriteLine("ESC Exit");
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
