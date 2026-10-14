using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TaskApi.Tests.Integration.Infrastructure;

internal class InMemoryLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception)));
}
