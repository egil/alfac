using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assimilated.Alfac.Utils
{
    /// <summary>
    /// A simple logger class that outputs to the console 
    /// and to a log file of the users choosing.
    /// </summary>
    public static class Logger
    {
        public static FileInfo DiskLog { get; set; }

        public static void UpdateProcessCounter(string text, params object[] args)
        {
            Console.Write(text, args);
        }
        public static void Info()
        {
            Info(string.Empty);
        }
        public static void Info(string text, params object[] args)
        {
            Console.WriteLine(text, args);         
            WriteToFile(text, args);
        }

        public static void Warning(string text, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(text, args);
            Console.ResetColor();
            WriteToFile(text, args);
        }

        public static void Error(string text, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(text, args);
            Console.ResetColor();
            WriteToFile(text, args);
        }

        public static void LogOffscreen(string text, params object[] args)
        {
            WriteToFile(text, args);
        }

        private static void WriteToFile(string text, params object[] args)
        {
            if (DiskLog != null)
            {
                using (TextWriter writer = new StreamWriter(DiskLog.FullName, true))
                {
                    writer.WriteLine(text, args);
                }
            }
        }
    }

}
