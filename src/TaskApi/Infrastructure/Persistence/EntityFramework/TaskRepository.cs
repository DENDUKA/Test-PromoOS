using Microsoft.EntityFrameworkCore;
using TaskApi.Common;
using TaskApi.Domain.Tasks;
using TaskApi.Features.Tasks.GetTasks;
using TaskApi.Infrastructure.Database;
using TaskApi.Infrastructure.Persistence.EntityFramework.Entities;

namespace TaskApi.Infrastructure.Persistence.EntityFramework;

public class TaskRepository(TaskDbContext db) : ITaskRepository
{
    private readonly Dictionary<Guid, (TaskEntity Entity, TaskItem Task)> _loaded = [];
    private readonly List<(TaskEntity Entity, TaskItem Task)> _added = [];

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        var entity = task.ToEntity();
        await db.Tasks.AddAsync(entity, cancellationToken);
        _added.Add((entity, task));
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var (entity, task) in _loaded.Values)
        {
            entity.CopyFrom(task);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(exception);
        }

        foreach (var (entity, task) in _added)
        {
            if (task.Id == Guid.Empty)
            {
                task.AssignId(entity.Id);
            }

            _loaded[entity.Id] = (entity, task);
        }

        _added.Clear();
    }

    public async Task<TaskItem?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var task = entity.ToDomain();
        _loaded[id] = (entity, task);

        return task;
    }

    public async Task<IReadOnlyList<TaskItem>> ListAsync(GetTasksQuery query, CancellationToken cancellationToken)
    {
        var tasks = db.Tasks
            .AsNoTracking()
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id);

        if (!query.IsPaged)
        {
            return (await tasks.ToListAsync(cancellationToken)).ConvertAll(entity => entity.ToDomain());
        }

        var skip = (long)(query.EffectivePage - 1) * query.EffectivePageSize;
        if (skip > int.MaxValue)
        {
            return [];
        }

        var page = await tasks
            .Skip((int)skip)
            .Take(query.EffectivePageSize)
            .ToListAsync(cancellationToken);

        return page.ConvertAll(entity => entity.ToDomain());
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
