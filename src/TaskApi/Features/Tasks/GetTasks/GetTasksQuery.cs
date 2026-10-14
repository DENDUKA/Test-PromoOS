namespace TaskApi.Features.Tasks.GetTasks;

public record GetTasksQuery(int? Page = null, int? PageSize = null)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public bool IsPaged => Page is not null || PageSize is not null;

    public int EffectivePage => Page ?? 1;

    public int EffectivePageSize => PageSize ?? DefaultPageSize;
}
