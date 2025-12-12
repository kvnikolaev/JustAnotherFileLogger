# Just Another File Logger
It's very simple implementation of the basic logging functionality to the file based on **.NET** logging infrastructure and written on the example of **Microsoft.Extensions.Logging.Console**.
The main goal is to provide a minimal and extensible implementation of logging to a file without using third-party libraries.

## ⚠ At the current stage the project is not completed.
Known limitations:

- FileLoggerProcessor - no choice overflow policy for the internal queue (new log entries are always get dropped)
- FileLoggerProvider  - no `OptionsMonitor`, only a default formatter, no `ScopeProvider`
- FileLogger          - no `IBufferedLogger` implementation
- FileLogFormatter    - no options, no `ScopeProvider`, not supported `BufferedLogRecord`

## 🐞 Known issues
If you initialize `FileLoggerProcessor` with **two `StreamWriter` instances created from the same file stream**, a **DisposedException** will be thrown during **Dispose()**, since both writers attempt to dispose the same underlying stream.
