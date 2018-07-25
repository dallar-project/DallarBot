using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace Dallar
{
    // @TODO: This should use a better system than simply logging to a file
    // Due to the numerous things we're logging, sometimes we get hit with a 'file in use' during a log
    // which throws an exception D:. We really need a better form of logging.
    public class LogHandlerService
    {
        public LogHandlerService()
        {
        }

        public static string CenterString(string value)
        {
            return String.Format("{0," + ((Console.WindowWidth / 2) + ((value).Length / 2)) + "}", value);
        }

        public static void Log(string log)
        {
            Debug.WriteLine(log);
            try
            {
                File.AppendAllText(Environment.CurrentDirectory + "/log.txt", log + Environment.NewLine);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to write log. " + e.Message);
            }
            
        }
    }
}
