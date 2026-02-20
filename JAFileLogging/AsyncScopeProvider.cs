using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace JAFileLogging
{
    internal class AsyncScopeProvider : IExternalScopeProvider
    {
        private AsyncLocal<Scope?> _scope = new AsyncLocal<Scope?>();

        public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
        {
            void Report(Scope? current)
            {
                if (current == null)
                {
                    return;
                }
                Report(current.Parent);
                callback(current.State, state);
            }

            Report(_scope.Value);
        }

        public IDisposable Push(object? state)
        {
            Scope? parent = _scope.Value;
            var newScope = new Scope(this, state, parent);
            _scope.Value = newScope;

            return newScope;
        }

        private class Scope : IDisposable
        {
            private AsyncScopeProvider _master;
            private bool _isDisposed;

            public Scope? Parent { get; }
            public object? State { get; }

            public Scope(AsyncScopeProvider master, object? state, Scope? parent)
            {
                this._master = master;
                this.Parent = parent;
                this.State = state;
            }

            public override string? ToString()
            {
                return State?.ToString();
            }
            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _master._scope.Value = Parent;
                    _isDisposed = true;
                }
            }
        }
    }
}
