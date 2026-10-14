using TaskApi.Domain.Common;

namespace TaskApi.Domain.Tasks.Events;

public sealed record TaskCompletedDomainEvent(
    Guid TaskId,
    string Title,
    DateTimeOffset CompletedAt,
    Priority Priority) : IDomainEvent
{
    public DateTimeOffset OccurredAt => CompletedAt;
}
