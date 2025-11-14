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
        private readonly ConcurrentDictionary<string, FileLogger> _loggers;
        private ConcurrentDictionary<string, FileLogFormatter> _formatters;
        private readonly FileLoggerProcessor _messageQueue;

        private string _filePath;
        private const string _logDirectory = "logs";

        public FileLoggerProvider(string? filePath /*optionMonitor*/)
        {
            if (filePath == null) throw new ArgumentException(string.Format("Argument {0} isn't supposed to be null", filePath));
            
            var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            StreamWriter file = new StreamWriter(fileStream) { AutoFlush = true };
            StreamWriter errorFile = new StreamWriter(fileStream) { AutoFlush = true };

            _filePath = filePath;
            _loggers = new ConcurrentDictionary<string, FileLogger>();
            _formatters = new ConcurrentDictionary<string, FileLogFormatter>();
            _formatters.TryAdd(FileLogFormatter.Name, new FileLogFormatter());
            _messageQueue = new FileLoggerProcessor(file, errorFile);

            // options
        }

        //public FileLoggerProvider() для автоматического создания файлов по дням
        //{
        //    var logPath = Path.Combine(AppContext.BaseDirectory, _logDirectory);
        //    if (!Directory.Exists(logPath))
        //        Directory.CreateDirectory(logPath);


        //}

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
