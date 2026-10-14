using TaskApi.Domain.Tasks;

namespace TaskApi.Infrastructure.Persistence.EntityFramework.Entities;

public static class TaskEntityMapper
{
    public static TaskItem ToDomain(this TaskEntity entity) => TaskItem.Restore(
        entity.Id,
        entity.Title,
        entity.IsCompleted,
        entity.CreatedAt,
        entity.CompletedAt,
        entity.Priority);

    public static TaskEntity ToEntity(this TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        IsCompleted = task.IsCompleted,
        CreatedAt = task.CreatedAt,
        CompletedAt = task.CompletedAt,
        Priority = task.Priority,
    };

    public static void CopyFrom(this TaskEntity entity, TaskItem task)
    {
        entity.Title = task.Title;
        entity.IsCompleted = task.IsCompleted;
        entity.CreatedAt = task.CreatedAt;
        entity.CompletedAt = task.CompletedAt;
        entity.Priority = task.Priority;
    }
}
