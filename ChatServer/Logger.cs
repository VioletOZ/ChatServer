using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ChatServer
{
    public static class Logger
    {
        // 로그 파일 생성
        //static readonly string DirPath = Environment.CurrentDirectory + "/logs";
        static string DirPath = Constance.ENV_CHAT_SERVER_LOG_PATH;
        static DirectoryInfo di = new DirectoryInfo(DirPath);

        public static void WriteLog(string str)
        {
            string temp;
            
            try
            {
                string FilePath = DirPath + "/" + DateTime.Today.ToString("yyyy-MM-dd") + ".log";
                
                FileInfo fi = new FileInfo(FilePath);

                if (!di.Exists) Directory.CreateDirectory(DirPath);
                if (!fi.Exists)
                {
                    using (StreamWriter sw = new StreamWriter(FilePath))
                    {
                        temp = string.Format("[{0}]{1}", DateTime.Now, str);
                        Console.WriteLine(temp);
                        sw.WriteLine(temp);                        
                        sw.Close();

                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(FilePath))
                    {
                        temp = string.Format("[{0}]{1}", DateTime.Now, str);
                        Console.WriteLine(temp);
                        sw.WriteLine(temp);
                        sw.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}