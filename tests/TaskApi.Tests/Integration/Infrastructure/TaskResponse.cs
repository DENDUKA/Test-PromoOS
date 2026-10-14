namespace TaskApi.Tests.Integration.Infrastructure;

public record TaskResponse(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string Priority);
