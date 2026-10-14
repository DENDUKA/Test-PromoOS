using Microsoft.EntityFrameworkCore;
using TaskApi.Application.Abstractions;
using TaskApi.Application.Tasks.GetTasks;
using TaskApi.Domain.Tasks;

namespace TaskApi.Infrastructure.Persistence;

public sealed class TaskRepository(TaskDbContext db) : ITaskRepository
{
    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken) =>
        await db.Tasks.AddAsync(task, cancellationToken);

    public Task<TaskItem?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<TaskPage> ListAsync(GetTasksQuery query, CancellationToken cancellationToken)
    {
        var filtered = db.Tasks.AsNoTracking();

        if (query.IsCompleted is { } isCompleted)
        {
            filtered = filtered.Where(t => t.IsCompleted == isCompleted);
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        // ThenBy(Id) делает порядок детерминированным, иначе страницы могут "плыть" при равных ключах.
        var ordered = query.SortBy switch
        {
            TaskSortBy.Priority => query.Descending
                ? filtered.OrderByDescending(t => t.Priority)
                : filtered.OrderBy(t => t.Priority),
            _ => query.Descending
                ? filtered.OrderByDescending(t => t.CreatedAt)
                : filtered.OrderBy(t => t.CreatedAt),
        };
        var stable = ordered.ThenBy(t => t.Id);

        if (!query.IsPaged)
        {
            return new TaskPage(await stable.ToListAsync(cancellationToken), totalCount);
        }

        var skip = (long)(query.EffectivePage - 1) * query.EffectivePageSize;
        if (skip > int.MaxValue)
        {
            return new TaskPage([], totalCount);
        }

        var items = await stable
            .Skip((int)skip)
            .Take(query.EffectivePageSize)
            .ToListAsync(cancellationToken);

        return new TaskPage(items, totalCount);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
