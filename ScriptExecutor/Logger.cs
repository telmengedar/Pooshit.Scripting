using System;

namespace NightlyCode.ScriptExecutor {
    public class Logger {
        
        void Log(string tag, string message, string details) {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss} {tag}: {message}");
            if (!string.IsNullOrEmpty(details))
                Console.WriteLine(details);
        }

        public void Info(string message, string details = null) {
            Log("INF", message, details);
        }

        public void Warning(string message, string details = null) {
            Log("WRN", message, details);
        }

        public void Error(string message, string details = null) {
            Log("ERR", message, details);
        }
        
        public void Error(string message, Exception details) {
            Log("ERR", message, details?.ToString());
        }
    }
}