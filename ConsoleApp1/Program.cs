using Microsoft.Extensions.Logging;
using System.Text;

namespace ConsoleApp1
{
    public sealed class AsyncLocalLogger
    {
        private readonly AsyncLocal<Scope?> _currentScope = new();

        public IDisposable BeginScope(object state)
        {
            var parent = _currentScope.Value;
            var newScope = new Scope(this, state, parent);
            _currentScope.Value = newScope;
            return newScope;
        }

        public void Log(string message)
        {
            var builder = new StringBuilder();

            AppendScopes(builder);

            builder.Append(message);

            Console.WriteLine(builder.ToString());
        }

        private void AppendScopes(StringBuilder builder)
        {
            var scope = _currentScope.Value;
            if (scope == null)
                return;

            var stack = new System.Collections.Generic.Stack<object>();

            while (scope != null)
            {
                stack.Push(scope.State);
                scope = scope.Parent;
            }

            while (stack.Count > 0)
            {
                builder.Append('[');
                builder.Append(stack.Pop());
                builder.Append("] ");
            }
        }

        private sealed class Scope : IDisposable
        {
            private readonly AsyncLocalLogger _logger;
            private bool _disposed;

            public object State { get; }
            public Scope? Parent { get; }

            public Scope(AsyncLocalLogger logger, object state, Scope? parent)
            {
                _logger = logger;
                State = state;
                Parent = parent;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                if (_logger._currentScope.Value == this)
                {
                    _logger._currentScope.Value = Parent;
                }
            }
        }
    }

    internal class Program
    {
        public static async Task Main()
        {
            var logger = new AsyncLocalLogger();

            using (logger.BeginScope("Request-1"))
            {
                logger.Log("Start");

                await Task.Delay(100);

                logger.Log("After await");

                await Task.WhenAll(
                    Task.Run(() =>
                    {
                        logger.BeginScope("Parallel-A");
                            logger.Log("Inside A");
                    }),
                    Task.Run(() =>
                    {
                        logger.BeginScope("Parallel-B");
                            logger.Log("Inside B");
                    })
                );

                logger.Log("End");
            }
        }

        //static void Main(string[] args)
        //{
        //    var logger = Logger.GetLogger(typeof(Program));
        //    Run(logger);
            
        //}

        static async void Run(ILogger logger)
        {
            

            logger.LogInformation("Before Scope");
            using var scope = logger.BeginScope("First Scope");
            logger.LogInformation("In Scope");
            var t = SomeClass.SomeOutMethod();
            logger.LogInformation("Again In First Scope");
            await t;
            Console.ReadLine();
        }

        class SomeClass {
            public static async Task SomeOutMethod()
            {
                var logger = Logger.GetLogger(typeof(SomeClass));
                using var t = logger.BeginScope("Nested Scope");
                await Task.Delay(1000);
                logger.LogInformation("In Nested Scope");
            }
        }
    }
}