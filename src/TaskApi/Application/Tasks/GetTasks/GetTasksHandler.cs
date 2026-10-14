using TaskApi.Application.Abstractions;
using TaskApi.Application.Common;

namespace TaskApi.Application.Tasks.GetTasks;

public enum TaskSortBy
{
    CreatedAt,
    Priority,
}

/// <summary>
/// Page и PageSize необязательны: если оба не заданы, возвращаются ВСЕ задачи (требование задания).
/// </summary>
public sealed record GetTasksQuery(
    int? Page = null,
    int? PageSize = null,
    bool? IsCompleted = null,
    TaskSortBy SortBy = TaskSortBy.CreatedAt,
    bool Descending = false)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public bool IsPaged => Page is not null || PageSize is not null;

    public int EffectivePage => Page ?? 1;

    public int EffectivePageSize => PageSize ?? DefaultPageSize;
}

public sealed record TaskListResult(IReadOnlyList<TaskDto> Items, int TotalCount);

public sealed class GetTasksHandler(ITaskRepository repository)
{
    public async Task<Result<TaskListResult>> HandleAsync(GetTasksQuery query, CancellationToken cancellationToken)
    {
        if (query.IsPaged)
        {
            if (query.EffectivePage < 1)
            {
                return Result<TaskListResult>.Failure(
                    Error.Validation("tasks.invalid_page", "Page must be greater than or equal to 1.", "page"));
            }

            if (query.EffectivePageSize is < 1 or > GetTasksQuery.MaxPageSize)
            {
                return Result<TaskListResult>.Failure(Error.Validation(
                    "tasks.invalid_page_size",
                    $"PageSize must be between 1 and {GetTasksQuery.MaxPageSize}.",
                    "pageSize"));
            }
        }

        var page = await repository.ListAsync(query, cancellationToken);
        var items = page.Items.Select(TaskDto.From).ToList();

        return Result<TaskListResult>.Success(new TaskListResult(items, page.TotalCount));
    }
}
