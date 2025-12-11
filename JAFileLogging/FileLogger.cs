using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JAFileLogging
{
    internal class FileLogger : ILogger//, IBufferedLogger
    {
        private FileLoggerProcessor _messageQueue;
        private readonly string _categoryName;

        internal FileLogFormatter Formatter { get; set; }

        public FileLogger(string categoryName, FileLoggerProcessor processor, FileLogFormatter formatter)
        {
            _categoryName = categoryName;
            _messageQueue = processor;
            Formatter = formatter;
        }


        [ThreadStatic]
        private static StringWriter? t_stringWriter;

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            ArgumentNullException.ThrowIfNull(formatter);

            t_stringWriter ??= new StringWriter();
            LogEntry<TState> logEntry = new LogEntry<TState>(logLevel, _categoryName, eventId, state, exception, formatter);
            Formatter.Write(in logEntry, /*ScopeProvider,*/ t_stringWriter);

            var sb = t_stringWriter.GetStringBuilder();
            if (sb.Length == 0)
            {
                return;
            }
            string computedAnsiString = sb.ToString();
            sb.Clear();
            if (sb.Capacity > 1024)
            {
                sb.Capacity = 1024;
            }
            _messageQueue.EnqueueMessage(new LogMessageEntry(computedAnsiString, LogAsError: logLevel >= LogLevel.Error /*logLevel >= Options.LogToStandardErrorThreshold*/));
        }

        // for IBufferedLogger
        /// <inheritdoc />
        //public void LogRecords(IEnumerable<BufferedLogRecord> records)
        //{
        //    ArgumentNullException.ThrowIfNull(records);

        //    StringWriter writer = t_stringWriter ??= new StringWriter();

        //    var sb = writer.GetStringBuilder();
        //    foreach (var rec in records)
        //    {
        //        var logEntry = new LogEntry<BufferedLogRecord>(rec.LogLevel, _name, rec.EventId, rec, null, static (s, _) => s.FormattedMessage ?? string.Empty);
        //        Formatter.Write(in logEntry, null, writer);

        //        if (sb.Length == 0)
        //        {
        //            continue;
        //        }

        //        string computedAnsiString = sb.ToString();
        //        sb.Clear();
        //        _queueProcessor.EnqueueMessage(new LogMessageEntry(computedAnsiString, logAsError: rec.LogLevel >= Options.LogToStandardErrorThreshold));
        //    }

        //    if (sb.Capacity > 1024)
        //    {
        //        sb.Capacity = 1024;
        //    }
        //}

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        internal void ChangeLoggingProcessor(FileLoggerProcessor processor)
        {
            this._messageQueue = processor;
        }
    }
}
