using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Console;
using JAFileLogging;


namespace ConsoleApp1
{
    internal class Logger
    {
        static ILoggerFactory _factory;
        static Dictionary<string, ILogger> _loggers;

        static Logger()
        {
            _factory = LoggerFactory.Create(builder =>
            {
                builder
                .AddConsole()
                .AddFile("log.txt")
                .AddDailyFile();
            });
            _loggers = new Dictionary<string, ILogger>();
        }

        public static ILogger GetLogger(Type type)
        {
            if (!_loggers.ContainsKey(type.Name))
                _loggers.Add(type.Name, _factory.CreateLogger(type));

            return _loggers[type.Name];
        }
    }
}
