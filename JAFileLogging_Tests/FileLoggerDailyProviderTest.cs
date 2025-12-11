using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFileLogging_Tests
{
    internal class FileLoggerDailyProviderTest
    {
        [SetUp]
        public void CleanLogDirectory()
        {
            var files = Directory.GetFiles(FileLoggerDailyProvider._logDirectory, "??-??-??.log");
            foreach(var file in files)
                File.Delete(file);
        }

        [Test]
        public void CreatesDailyFileNames()
        {
            // Arrange
            FileLoggerDailyProvider provider = new FileLoggerDailyProvider();
            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("First log");

            int offset = 1;

            // Act
            provider.ForceChangeLogFilePath(DateTime.Now.AddDays(offset++));
            logger.LogInformation("Second log");

            provider.ForceChangeLogFilePath(DateTime.Now.AddDays(offset++));
            logger.LogInformation("Third log");

            // Assert
            var logFiles = Directory.GetFiles(FileLoggerDailyProvider._logDirectory, "*.log");
            Assert.That(logFiles.Length, Is.EqualTo(offset));
        }
    }
}
