using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JAFileLogging
{
    /// <summary>
    /// A provider of <see cref="FileLogger"/> instances.
    /// </summary>
    internal class FileLoggerProvider : ILoggerProvider//, ISupportExternalScope
    {
        private ConcurrentDictionary<string, FileLogFormatter> _formatters;
        protected ConcurrentDictionary<string, FileLogger> _loggers;
        protected FileLoggerProcessor _messageQueue;

        private string _filePath;

        public FileLoggerProvider(string? filePath /*optionMonitor*/)
        {
            if (filePath == null) throw new ArgumentException(string.Format("Argument {0} isn't supposed to be null", filePath));

            _filePath = filePath;
            _loggers = new ConcurrentDictionary<string, FileLogger>();
            _formatters = new ConcurrentDictionary<string, FileLogFormatter>();
            _formatters.TryAdd(FileLogFormatter.Name, new FileLogFormatter());
            InitMessageProcessor(filePath);

            // options
        }

        protected void InitMessageProcessor(string filePath)
        {
            var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            StreamWriter file = new StreamWriter(fileStream) { AutoFlush = true };
            //StreamWriter errorFile = new StreamWriter(fileStream) { AutoFlush = true };
            _messageQueue = new FileLoggerProcessor(file, file);

            foreach (var logger in _loggers)
                logger.Value.ChangeLoggingProcessor(_messageQueue);
        }


        public ILogger CreateLogger(string categoryName)
        {
            //_options.CurrentValue.FormatterName
            var logFormatter = _formatters[FileLogFormatter.Name];

            return _loggers.TryGetValue(categoryName, out FileLogger? logger) ?
                logger :
                _loggers.GetOrAdd(categoryName, new FileLogger(categoryName, _messageQueue, logFormatter/*, _scopeProvider, _options.CurrentValue*/));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _messageQueue.Dispose();
        }

    }
}
