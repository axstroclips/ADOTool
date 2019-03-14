using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ADO_Tool
{
    public class WriteLog
    {
        public static string GetFileNameByDate(string fileName)
        {
            string timestamp = string.Format("{0:d/M/yyyy}", DateTime.Now);
            string formatFileName = fileName + "_" + timestamp.Replace(@"/", "_").Replace(" ", "_").Replace(":", "_");
            return formatFileName;
        }

        public static string GetTimeStamp()
        {
            string timestamp = string.Format("{0:d/M/yyyy HH:mm:ss}", DateTime.Now);
            return timestamp;
        }

        public static void WriteLogLine(string message, params object[] args)
        {
            using (StreamWriter writer = new StreamWriter(GetFileNameByDate("ADO_Tool_Log")+".txt",true))
            {
                writer.WriteLine(string.Format("{0}: {1}", GetTimeStamp(), message), args);
                writer.Flush();
            }
        }
    }
}
