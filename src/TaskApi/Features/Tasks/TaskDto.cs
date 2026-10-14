using TaskApi.Domain.Tasks;

namespace TaskApi.Features.Tasks;

public record TaskDto(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Priority Priority)
{
    public static TaskDto From(TaskItem task) => new(
        task.Id,
        task.Title,
        task.IsCompleted,
        task.CreatedAt,
        task.CompletedAt,
        task.Priority);
}
