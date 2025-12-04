using Zerra.Logging;

namespace Pets.Service
{
    public sealed class Logger : ILog
    {
        public void Critical(string? message = null, Exception? ex = null)
        {
            Console.WriteLine($"CRITICAL: {message} {ex}");
        }

        public void Critical(Exception? ex = null)
        {
            Console.WriteLine($"CRITICAL: {ex}");
        }

        public void Debug(string message)
        {
            Console.WriteLine($"DEBUG: {message}");
        }

        public void Error(string? message = null, Exception? ex = null)
        {
            Console.WriteLine($"ERROR: {message} {ex}");
        }

        public void Error(Exception? ex = null)
        {
            Console.WriteLine($"ERROR: {ex}");
        }

        public void Info(string message)
        {
            Console.WriteLine($"INFO: {message}");
        }

        public void Trace(string message)
        {
            Console.WriteLine($"TRACE: {message}");
        }

        public void Warn(string message)
        {
            Console.WriteLine($"WARN: {message}");
        }
    }
}
