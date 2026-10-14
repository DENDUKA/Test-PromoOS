using TaskApi.Application.Tasks.GetTasks;
using TaskApi.Domain.Tasks;

namespace TaskApi.Application.Abstractions;

public sealed record TaskPage(IReadOnlyList<TaskItem> Items, int TotalCount);

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    /// <summary>Возвращает отслеживаемую сущность (с Version для оптимистичной блокировки).</summary>
    Task<TaskItem?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<TaskPage> ListAsync(GetTasksQuery query, CancellationToken cancellationToken);

    /// <summary>Жёсткое удаление. Возвращает false, если задачи не было.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
