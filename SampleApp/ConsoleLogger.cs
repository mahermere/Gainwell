using System;
using System.IO;
using System.Text;
using SampleApp.Logging;

namespace SampleApp.Logging
{
    public class LoggingConsoleWriter : TextWriter
    {
        private readonly TextWriter _originalOut;
        private readonly Logger _logger;

        public LoggingConsoleWriter(TextWriter originalOut)
        {
            _originalOut = originalOut;
            _logger = Logger.Instance;
        }

        public override Encoding Encoding => _originalOut.Encoding;

        public override void WriteLine(string? value)
        {
            // Write to original console
            _originalOut.WriteLine(value);

            // Log to file
            _logger.LogConsoleOutput(value ?? string.Empty);
        }

        public override void WriteLine()
        {
            _originalOut.WriteLine();
            _logger.LogConsoleOutput(string.Empty);
        }

        public override void WriteLine(object? value)
        {
            var stringValue = value?.ToString() ?? string.Empty;
            _originalOut.WriteLine(stringValue);
            _logger.LogConsoleOutput(stringValue);
        }

        public override void WriteLine(string format, params object?[] args)
        {
            var formattedValue = string.Format(format, args);
            _originalOut.WriteLine(formattedValue);
            _logger.LogConsoleOutput(formattedValue);
        }

        public override void Write(string? value)
        {
            _originalOut.Write(value);
            // For Write operations, we'll log immediately to avoid partial messages
            _logger.LogConsoleOutput($"[PARTIAL] {value ?? string.Empty}");
        }

        public override void Write(char value)
        {
            _originalOut.Write(value);
            _logger.LogConsoleOutput($"[CHAR] {value}");
        }

        public override void Flush()
        {
            _originalOut.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _originalOut?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public static class ConsoleLogger
    {
        private static LoggingConsoleWriter? _loggingWriter;
        private static bool _isEnabled = false;

        public static void EnableConsoleLogging()
        {
            if (!_isEnabled)
            {
                _loggingWriter = new LoggingConsoleWriter(Console.Out);
                Console.SetOut(_loggingWriter);
                _isEnabled = true;

                // Log that console logging has been enabled
                Logger.Instance.Info("Console logging enabled - all Console.WriteLine calls will be logged to file");
            }
        }

        public static void DisableConsoleLogging()
        {
            if (_isEnabled && _loggingWriter != null)
            {
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                _loggingWriter.Dispose();
                _loggingWriter = null;
                _isEnabled = false;
            }
        }

        public static bool IsEnabled => _isEnabled;
    }
}