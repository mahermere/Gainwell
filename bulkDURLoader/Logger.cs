using System;
using System.IO;
using System.Threading;

namespace bulkDURLoader.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class Logger
    {
        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => _instance.Value;

        private readonly string _logDirectory;
        private readonly string _logFileName;
        private readonly object _lockObject = new object();

        private Logger()
        {
            _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "log");
            _logFileName = $"app_{DateTime.Now:yyyyMMdd}.log";

            // Ensure log directory exists
            Directory.CreateDirectory(_logDirectory);
        }

        public void Log(LogLevel level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level.ToString().ToUpper()}] {message}";

            lock (_lockObject)
            {
                try
                {
                    var logFilePath = Path.Combine(_logDirectory, _logFileName);
                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Fallback: write to console if file logging fails
                    System.Console.WriteLine($"[LOGGER ERROR] Failed to write to log file: {ex.Message}");
                }
            }
        }

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message) => Log(LogLevel.Error, message);
        public void Error(string message, Exception ex) => Log(LogLevel.Error, $"{message} - Exception: {ex}");

        public string GetLogFilePath()
        {
            return Path.Combine(_logDirectory, _logFileName);
        }

        public void LogConsoleOutput(string message)
        {
            Log(LogLevel.Info, $"[CONSOLE] {message}");
        }
    }
}