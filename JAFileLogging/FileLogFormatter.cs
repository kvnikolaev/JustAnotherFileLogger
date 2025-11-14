using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFileLogging
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter
    /// </summary>
    internal class FileLogFormatter //: IDisposable
    {
        public const string Name = "default";

        private const string textSeparator = " - ";
        private const string leftPadding = "      ";

        public FileLogFormatter(/*IOptionsMonitor<CustomOptions> options*/)
        {
        }

        public virtual void Write<TState>(
            in LogEntry<TState> logEntry,
            //IExternalScopeProvider? scopeProvider,
            TextWriter textWriter)
        {
            string? message = logEntry.Formatter?.Invoke(
                    logEntry.State, logEntry.Exception);
            if (logEntry.Exception == null && message == null)
            {
                return;
            }

            string? exceptionString = default;
            if (logEntry.Exception != null)
            {
                exceptionString = logEntry.Exception!.ToString();
                exceptionString += Environment.NewLine;
                exceptionString += logEntry.Exception.StackTrace;
            }
            WriteInternal(textWriter, message ?? "", logEntry.LogLevel, logEntry.EventId.Id, exceptionString, logEntry.Category, GetCurrentDateTime());
        }

        private void WriteInternal(TextWriter textWriter, string message, LogLevel logLevel,
            int eventId, string? exception, string category, DateTimeOffset stamp)
        {
            string level = GetLogLevelString(logLevel);
            textWriter.Write(level);
            textWriter.Write(":");

            string time = string.Format(" [{0}] ", stamp.ToString("HH:mm:fff")); // FormatterOptions.TimestampFormat
            textWriter.Write(time);

            textWriter.Write("eventId=");
            textWriter.Write(eventId.ToString());

            textWriter.Write(textSeparator);
            textWriter.Write(category);
            textWriter.Write(textSeparator);

            textWriter.Write(message);
            if (exception != null)
            {
                textWriter.Write(Environment.NewLine);
                textWriter.Write(leftPadding);
                textWriter.Write(exception);
            }
            textWriter.Write(Environment.NewLine);
        }

        private DateTimeOffset GetCurrentDateTime()
        {
            return DateTimeOffset.Now;
        }

        public string GetLogLevelString(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };

        //public void Dispose() => _optionsReloadToken?.Dispose();
    }
}
