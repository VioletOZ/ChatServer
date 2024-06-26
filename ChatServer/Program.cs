﻿using System;
using System.Net;
using System.Net.Sockets;
using WebSocketSharp;
using WebSocketSharp.Server;
using StackExchange.Redis;
using StackExchange.Redis.MultiplexerPool;

using System.Threading;
using System.Threading.Tasks;
using System.IO;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatServer
{
    

    class Program
    {
        static void Main(string[] args)
        {
            StreamReader r = new StreamReader("cfg/ChatServer.cfg");
            Constance.Env = JsonSerializer.Deserialize<Env>(r.ReadToEnd());
            
            Console.WriteLine(Constance.Env.ChatServerPort);            

            Console.WriteLine(Constance.Env.ChatServerRedisDBServerIP);

            Console.WriteLine(Constance.Env.GameServerRedisDBServerIP);

            if (Constance.Env.ChatServerPort == null)
            {
                Console.WriteLine("Env Server Port is Null");
                Environment.Exit(0);
            }

            // 게임서버 레디스 임시로 막아둠 ... Chat/AuthVerify 도 막아둠 
            // 게임서버 레디스 접속 
            bool result = RedisManager.Instance.ConnectGameServerRedis();
            if (!result)
            {
                Logger.WriteLog("GameServer Redis Connect Fail");
                Environment.Exit(0);
            }

            //var pong = RedisManager.Instance.gameServerState.db.Ping();
            //Console.WriteLine(pong);
            //Logger.WriteLog("GameServer Redis Connect!!");

            int minWorker = Environment.ProcessorCount * 2;
            int minIOC;
            if (ThreadPool.SetMinThreads(minWorker, minWorker))
            {
                ThreadPool.GetMinThreads(out minWorker, out minIOC);
                Logger.WriteLog("MinWorker : " + minWorker + "-" + minIOC);
            }
            else
            {
                // ...??
            }


            Logger.WriteLog("ServerLog Path : " + Constance.Env.ChatServerLogPath);
            // 웹소켓 초기화
            WebSocketServer webSocketServer = null;
            if (webSocketServer != null)
                return;

            int port = Int32.Parse(Constance.Env.ChatServerPort);
            webSocketServer = new WebSocketServer(port);
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
            Console.WriteLine("ServerStart - " + ipAddr + ":" + webSocketServer.Port);
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
