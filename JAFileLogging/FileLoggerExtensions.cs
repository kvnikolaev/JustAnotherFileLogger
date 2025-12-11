using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFileLogging
{
    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string? filePath = null)
        {
            //builder.AddConfiguration();

            //builder.AddConsoleFormatter

            //builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());

            builder.AddProvider(new FileLoggerProvider(filePath));

            return builder;
        }

        public static ILoggingBuilder AddDailyFile(this ILoggingBuilder builder)
        {
            builder.AddProvider(new FileLoggerDailyProvider());
            return builder;
        }
        
    }
}
