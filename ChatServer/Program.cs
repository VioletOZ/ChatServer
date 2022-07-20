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
            if (Constance.ENV_CHAT_SERVER_PORT == null)
            {
                Console.WriteLine("Env Server Port is Null");
                Environment.Exit(0);
            }

            Console.WriteLine(Constance.ENV_CHAT_SERVER_REDIS_ADDR);
            Console.WriteLine(Constance.ENV_CHAT_SERVER_REDIS_PORT);

            Console.WriteLine(Constance.ENV_GAME_SERVER_REDIS_ADDR);
            Console.WriteLine(Constance.ENV_GAME_SERVER_REDIS_PORT);

            // 게임서버 레디스 임시로 막아둠 ... Chat/AuthVerify 도 막아둠 
            // 게임서버 레디스 접속 
            //bool result = RedisManager.Instance.ConnectGameServerRedis();
            //if (!result)
            //{
            //    Logger.WriteLog("GameServer Redis conn Fail");
            //    Environment.Exit(0);
            //}

            //var pong = RedisManager.Instance.gameServerState.db.Ping();
            //Console.WriteLine(pong);
            //Logger.WriteLog("GameServer Redis Connect!!");

            int minWorker, minIOC;
            // Get the current settings.            
            ThreadPool.GetMinThreads(out minWorker, out minIOC);
            // Change the minimum number of worker threads to four, but
            // keep the old setting for minimum asynchronous I/O 
            // completion threads.
            Logger.WriteLog("MinWorker : " + minWorker + "-" + minIOC);
            if (ThreadPool.SetMinThreads(100, 10))
            {
                ThreadPool.GetMinThreads(out minWorker, out minIOC);
                Logger.WriteLog("MinWorker : " + minWorker + "-" + minIOC);
                // The minimum number of threads was set successfully.
            }
            else
            {
                // The minimum number of threads was not changed.
            }


            Logger.WriteLog("ServerLog Path : " + Constance.ENV_CHAT_SERVER_LOG_PATH);
            // 웹소켓 초기화
            WebSocketServer webSocketServer = null;
            if (webSocketServer != null)
                return;

            webSocketServer = new WebSocketServer(9000);
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
            while (true)
            {

                Thread.Sleep(1000);
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
