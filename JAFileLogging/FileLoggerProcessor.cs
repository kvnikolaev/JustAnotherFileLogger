using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFileLogging
{
    internal class FileLoggerProcessor : IDisposable
    {
        private readonly Queue<LogMessageEntry> _messageQueue;
        private volatile int _messagesDropped;
        private bool _isAddingCompleted;
        private const int _maxQueuedMessages = 2500; //!! что будет если превысить?

        private readonly Thread _outputThread;

        private StreamWriter _errorOut;
        private StreamWriter _defaultOut;

        public FileLoggerProcessor(StreamWriter outFile, StreamWriter errorFile)
        {
            _messageQueue = new Queue<LogMessageEntry>();
            _errorOut = errorFile;
            _defaultOut = outFile;
            // Start Console message queue processor
            _outputThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "File logger queue processing thread"
            };
            _outputThread.Start();
        }

        public virtual void EnqueueMessage(LogMessageEntry message)
        {
            // cannot enqueue when adding is completed
            if (!Enqueue(message))
            {
                WriteMessage(message);
            }
        }

        internal void WriteMessage(LogMessageEntry entry)
        {
            try
            {
                var fileOut = entry.LogAsError ? _errorOut : _defaultOut;
                fileOut.Write(entry.Message);
            }
            catch
            {
                CompleteAdding();
            }
        }

        private void ProcessLogQueue()
        {
            while (TryDequeue(out LogMessageEntry message))
            {
                WriteMessage(message);
            }
        }

        public bool Enqueue(LogMessageEntry item) //!! добавить лог сообщения о переполнении очереди
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count >= _maxQueuedMessages && !_isAddingCompleted)
                {
                    _messagesDropped++;

                    Debug.Assert(_messageQueue.Count >= _maxQueuedMessages, "Log message queue overflow.");
                    Monitor.Wait(_messageQueue);
                }

                if (!_isAddingCompleted)
                {
                    Debug.Assert(_messageQueue.Count < _maxQueuedMessages);
                    bool startedEmpty = _messageQueue.Count == 0;
                    if (_messagesDropped > 0)
                    {
                        _messageQueue.Enqueue(new LogMessageEntry(
                            Message: $"Log message queue overflow. Messages droped {_messagesDropped}.",
                            LogAsError: true
                        ));

                        _messagesDropped = 0;
                    }

                    // if we just logged the dropped message warning this could push the queue size to
                    // MaxLength + 1, that shouldn't be a problem. It won't grow any further until it is less than
                    // MaxLength once again.
                    _messageQueue.Enqueue(item);

                    // if the queue started empty it could be at 1 or 2 now
                    if (startedEmpty)
                    {
                        // pulse for wait in Dequeue
                        Monitor.PulseAll(_messageQueue);
                    }

                    return true;
                }
            }

            return false;
        }

        public bool TryDequeue(out LogMessageEntry item)
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count == 0 && !_isAddingCompleted)
                {
                    Monitor.Wait(_messageQueue);
                }

                if (_messageQueue.Count > 0)
                {
                    item = _messageQueue.Dequeue();
                    if (_messageQueue.Count == _maxQueuedMessages - 1)
                    {
                        // pulse for wait in Enqueue
                        Monitor.PulseAll(_messageQueue);
                    }

                    return true;
                }

                item = default;
                return false;
            }
        }

        public void Dispose()
        {
            CompleteAdding();

            try
            {
                _outputThread.Join(1500);
            }
            catch (ThreadStateException) { }
            finally
            {
                _errorOut.Dispose();
                _defaultOut.Dispose();
            }
        }

        private void CompleteAdding()
        {
            lock (_messageQueue)
            {
                _isAddingCompleted = true;
                Monitor.PulseAll(_messageQueue);
            }
        }
    }
}
