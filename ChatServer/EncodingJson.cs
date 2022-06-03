using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace ChatServer
{
    public static class EncodingJson
    {
        public static string Serialize<T>(this T obj) 
        { 
            return JsonSerializer.Serialize<T>(obj, new JsonSerializerOptions()); 
        }

        public static object Deserialize<T>(Stream obj)
        {
            return JsonSerializer.Deserialize<T>(obj);
        }

    }
}
