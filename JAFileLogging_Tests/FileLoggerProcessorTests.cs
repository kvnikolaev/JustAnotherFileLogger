using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace JAFileLogging_Tests
{
    public class Tests
    {
        private const string _testFileName = "TestLog.log";
        private const string _errorFileName = "ErrorLog.log";
        private const string _categoryName = "TestCategory";

        [SetUp]
        public void Setup()
        {
        }
        
        StreamWriter testFileOut;

        [Test]
        public void SimpleLogWriting()
        {
            // Arrange
            var fileStream = new FileStream(_testFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            testFileOut = new StreamWriter(fileStream) { AutoFlush = true };
            using var logProcessor = new FileLoggerProcessor(testFileOut, testFileOut);

            var linesToLog = new[]
            {
                "First log",
                "Second log",
                "Third log",
                "Fourth log",
                "Fifth log"
            };

            // Act
            foreach(var line in linesToLog)
            {
                logProcessor.EnqueueMessage(new LogMessageEntry { Message = line + Environment.NewLine });
            }
            logProcessor.Dispose();

            // Assert
            var result = File.ReadAllLines(_testFileName);
            for (int i = 0; i < linesToLog.Length; i++)
            {
                Assert.True(result[i] == linesToLog[i]);
            }
        }

        [Test]
        public void AllLogsCount_WhenQueueOverflow()
        {
            // Arrange
            var fileStream = new FileStream(_testFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            testFileOut = new StreamWriter(fileStream) { AutoFlush = true };
            int iterations = 10;

            // Act
            using (var logProcessor = new FileLoggerProcessor(testFileOut, testFileOut))
            {
                logProcessor.MaxQueuedMessages = 3;
                for (int i = 0; i < iterations; i++)
                {
                    logProcessor.EnqueueMessage(new LogMessageEntry { Message = $"{i + 1} message" + Environment.NewLine });
                }
            }
            //Thread.Sleep(10000);

            //Assert
            var logs = File.ReadAllLines(_testFileName);
            int logCount = logs.Count(x => x.Split(' ').Count() == 2);
            int dropedCount = logs.Where(x => x.Contains("Messages droped")).Select(x => x.Split(' ').Last().Remove(1)).Sum(x => int.Parse(x));

            int resultLogsCount = logCount + dropedCount;
            Assert.True(resultLogsCount == iterations);

        }

        [Test]
        public void NotThrowsAndNotWrite_AfterDisposed()
        {
            // Arrange
            var fileStream = new FileStream(_testFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            testFileOut = new StreamWriter(fileStream) { AutoFlush = true };
            var logProcessor = new FileLoggerProcessor(testFileOut, testFileOut);
            var logger = new FileLogger("TestCategory", logProcessor, new FileLogFormatter());
            // Act
            logProcessor.Dispose();
            logger.LogInformation("Logging after dispose");
            // Assert
            var writenLogs = File.ReadAllLines(_testFileName);
            Assert.That(writenLogs.Length, Is.EqualTo(0));
        }

        [Test]
        public void LogsFlushed_AfterDispose()
        {
            // Arrange
            var fileStream = new FileStream(_testFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            testFileOut = new StreamWriter(fileStream) { AutoFlush = false };  // AutoFlush must be false for dispose test
            var logProcessor = new FileLoggerProcessor(testFileOut, testFileOut);
            var logger = new FileLogger(_categoryName, logProcessor, new FileLogFormatter());
            
            // Act
            const int repetitions = 1000;
            for (int i = 0; i < repetitions; i++)
            {
                logger.LogInformation("Message #{i}", i);
            }
            Thread.Sleep(1000); // When debug logProcessor may not empty queue in time without delay
            logProcessor.Dispose();

            // Assert
            Assert.True(CountLines(_testFileName) == repetitions);
        }
        
        [TestCase(-1)]
        [TestCase(0)]
        public void MaxQueueLength_SetInvalid_Throws(int invalidMaxQueueLength)
        {
            // Arrange
            using var processor = new FileLoggerProcessor(testFileOut, testFileOut);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => processor.MaxQueuedMessages = invalidMaxQueueLength);
        }

        [TestCase(true)]
        public void LogNotificationAsError_WhenQueueIsFull(bool okToDrop)
        {
            // Arrange
            var fileStream = new FileStream(_testFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            testFileOut = new StreamWriter(fileStream) { AutoFlush = true };
            var errorFileOut = new StreamWriter(new FileStream(_errorFileName, FileMode.Create, FileAccess.Write, FileShare.Read));

            using var processor = new FileLoggerProcessor(testFileOut, errorFileOut);
            processor.MaxQueuedMessages = 3;

            var logger = new FileLogger(_categoryName, processor, new FileLogFormatter());
            string messageTemplate = string.Join(", ", Enumerable.Range(1, 100).Select(x => "{A" + x + "}"));
            object[] messageParams = Enumerable.Range(1, 100).Select(x => (object)x).ToArray();

            // Act
            for (int i = 0; i < 20000; i++)
            {
                logger.LogInformation(messageTemplate, messageParams);
            }
            processor.Dispose();

            // Assert
            if (okToDrop)
            {
                Assert.True(CountLines(_errorFileName) > 0);
            }
            else
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ThrowDuringProcessLog_ShutsDownGracefully()
        {
            // Arrange
            var fileStream = new FileStream(_testFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            testFileOut = new TimesWriteCalledStream(fileStream) { AutoFlush = true };
            var errorFileOut = new WriteThrowingStream(new FileStream(_errorFileName, FileMode.Create, FileAccess.Write, FileShare.Read));
            using var processor = new FileLoggerProcessor(testFileOut, errorFileOut);

            var logger = new FileLogger(_categoryName, processor, new FileLogFormatter());

            // Act
            logger.LogInformation("Process 1st log normally using {0}", _testFileName);
            logger.LogInformation("Process 2nd log normally using {0}", _testFileName);
            while ((testFileOut as TimesWriteCalledStream)!.TimesWriteCalled != 2) ; // wait until the logs are processed
            Assert.True(2 == (testFileOut as TimesWriteCalledStream)!.TimesWriteCalled);

            logger.LogError("Causing exception to throw in {ClassName} using {DesiredConsole}", nameof(FileLoggerProcessor), _errorFileName);
            logger.LogInformation("After the write logic threw exception, {ClassName} stopped gracefully, finish processing queued logs", nameof(FileLoggerProcessor));
            // disposing makes sure that all queued messages are flushed
            processor.Dispose();

            // Assert
            Assert.That(CountLines(_testFileName), Is.EqualTo(3));
        }

        #region Test helpers
        private class TimesWriteCalledStream : StreamWriter
        {
            public int TimesWriteCalled { get; set; } = 0;

            public TimesWriteCalledStream(Stream stream) : base(stream)
            {
            }

            public override void Write(string? value)
            {
                base.Write(value);
                TimesWriteCalled++;
            }
        }

        private class WriteThrowingStream : StreamWriter
        {
            public WriteThrowingStream(Stream stream) : base(stream)
            {
            }

            public override void Write(string? value)
            {
                throw new InvalidOperationException();
            }
        }
        #endregion

        [Ignore("Performance test")]
        public void TimeTest()
        {
            for(int i = 0; i < 10000; i++)
            {
                var t = AltCountLines(_testFileName);
            }
        }

        private static long AltCountLines(string filePath)
        {
            long lineCount = 0;

            byte[] array = new byte[1024 * 1024]; // 1 MB buffer
            Span<byte> buffer = new Span<byte>(array);
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            int bytesRead;
            while ((bytesRead = fs.Read(buffer)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == '\n')
                        lineCount++;
                }
            }

            return lineCount;
        }

        private static long CountLines(string filePath)
        {
            var t = File.ReadLines(filePath).Count();
            return t;
        }
    }
}