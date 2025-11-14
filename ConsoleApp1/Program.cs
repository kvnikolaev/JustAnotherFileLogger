using Microsoft.Extensions.Logging;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var logger = Logger.GetLogger(typeof(Program));

            logger.LogInformation("In start!");
            logger.LogError("In processing...");
            logger.LogCritical("And Done!");
            logger.Log(LogLevel.Error, new ArgumentException("Empty args"), "Just failed");
            Console.ReadLine();
        }
    }
}