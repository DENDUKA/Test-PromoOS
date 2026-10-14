using TaskApi.Domain.Tasks;

namespace TaskApi.Application.Tasks;

public sealed record TaskDto(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Priority Priority)
{
    public static TaskDto From(TaskItem task) => new(
        task.Id,
        task.Title.Value,
        task.IsCompleted,
        task.CreatedAt,
        task.CompletedAt,
        task.Priority);
}
