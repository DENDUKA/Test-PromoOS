using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TaskApi.Tests.Integration.Infrastructure;

public class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyCollection<LogEntry> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new InMemoryLogger(categoryName, _entries);

    public void Dispose()
    {
    }
}
