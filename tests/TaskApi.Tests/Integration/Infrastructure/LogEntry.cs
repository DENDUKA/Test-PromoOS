using Microsoft.Extensions.Logging;

namespace TaskApi.Tests.Integration.Infrastructure;

public record LogEntry(string Category, LogLevel Level, string Message);
