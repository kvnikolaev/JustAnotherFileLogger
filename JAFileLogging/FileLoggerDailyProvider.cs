using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFileLogging
{
    internal class FileLoggerDailyProvider : FileLoggerProvider
    {
        internal const string _logDirectory = "logs";

        private System.Timers.Timer _dateChangedTimer;

        public FileLoggerDailyProvider() : base(GetDailyLogFilePath())
        {
            var timeBeforeMidnight = DateTime.Today.AddDays(1) - DateTime.Now;
            _dateChangedTimer = new System.Timers.Timer(timeBeforeMidnight);
            _dateChangedTimer.Elapsed += _dateChangedTimer_Elapsed;
            _dateChangedTimer.Start();
        }

        private void _dateChangedTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var filePath = GetDailyLogFilePath();
            base._messageQueue.Dispose();
            base.InitMessageProcessor(filePath);

            _dateChangedTimer.Interval = (DateTime.Today.AddDays(1) - DateTime.Now).Milliseconds;
            _dateChangedTimer.Start();
        }

        private static string GetDailyLogFilePath()
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, _logDirectory);
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            var logFilePath = Path.Combine(logPath, DateTime.Now.ToString("dd-MM-yy") + ".log");
            return logFilePath;
        }

        internal void ForceChangeLogFilePath(DateTime date)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, _logDirectory);
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            var logFilePath = Path.Combine(logPath, date.ToString("dd-MM-yy") + ".log");

            base._messageQueue.Dispose();
            base.InitMessageProcessor(logFilePath);
        }
    }
}
