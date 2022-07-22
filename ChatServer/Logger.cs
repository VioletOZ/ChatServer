using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;

namespace ChatServer
{
    public static class Logger
    {
        // 로그 파일 생성
        //static readonly string DirPath = Environment.CurrentDirectory + "/logs";
        static string DirPath = Constance.Env.ChatServerLogPath;
        static DirectoryInfo di = new DirectoryInfo(DirPath);

        public static void WriteLog(string str)
        {
            BlockingCollection<string> temp = new BlockingCollection<string>();

            try
            {
                string FilePath = DirPath + "/" + DateTime.Today.ToString("yyyy-MM-dd") + ".log";
                
                FileInfo fi = new FileInfo(FilePath);

                if (!di.Exists) Directory.CreateDirectory(DirPath);
                if (!fi.Exists)
                {
                    using (StreamWriter sw = new StreamWriter(FilePath))
                    {
                        temp.Add(string.Format("[{0}]{1}", DateTime.Now, str));
                        sw.WriteLine(temp.Take());
                        sw.Close();

                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(FilePath))
                    {
                        temp.Add(string.Format("[{0}]{1}", DateTime.Now, str));
                        sw.WriteLine(temp.Take());
                        sw.Close();
                    }
                }
            }
            catch 
            {
                
            }
        }
    }
}