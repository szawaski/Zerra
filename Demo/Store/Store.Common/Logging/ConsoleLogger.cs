using Zerra.Logging;

namespace Store.Common.Logging
{
    /// <summary>
    /// Writes log lines to the console, prefixed with the time and colored by level.
    /// </summary>
    public sealed class ConsoleLogger : ILogger
    {
        private static readonly Lock consoleLock = new();

        public void Trace(string message) => Write(ConsoleColor.DarkGray, "TRACE", message, null);
        public void Debug(string message) => Write(ConsoleColor.DarkGray, "DEBUG", message, null);
        public void Info(string message) => Write(ConsoleColor.Gray, "INFO", message, null);
        public void Warn(string message) => Write(ConsoleColor.Yellow, "WARN", message, null);
        public void Error(string? message = null, Exception? ex = null) => Write(ConsoleColor.Red, "ERROR", message, ex);
        public void Error(Exception? ex = null) => Write(ConsoleColor.Red, "ERROR", null, ex);
        public void Critical(string? message = null, Exception? ex = null) => Write(ConsoleColor.Magenta, "CRITICAL", message, ex);
        public void Critical(Exception? ex = null) => Write(ConsoleColor.Magenta, "CRITICAL", null, ex);

        internal static void Write(ConsoleColor color, string level, string? message, Exception? ex)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{DateTime.Now:HH:mm:ss.fff} ");
                Console.ForegroundColor = color;
                Console.Write($"{level,-5} ");
                Console.ResetColor();
                Console.WriteLine(ex is null ? message : $"{message} {ex.GetBaseException().Message}");
            }
        }
    }
}
