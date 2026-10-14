using FluentValidation;
using TaskApi.Common;
using TaskApi.Infrastructure.Database;

namespace TaskApi.Features.Tasks;

/// <summary>
/// Page и PageSize необязательны: если оба не заданы, возвращаются ВСЕ задачи (требование задания).
/// </summary>
public sealed record GetTasksQuery(int? Page = null, int? PageSize = null)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public bool IsPaged => Page is not null || PageSize is not null;

    public int EffectivePage => Page ?? 1;

    public int EffectivePageSize => PageSize ?? DefaultPageSize;
}

public sealed class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
{
    public GetTasksQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be greater than or equal to 1.")
            .When(query => query.Page.HasValue);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, GetTasksQuery.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {GetTasksQuery.MaxPageSize}.")
            .When(query => query.PageSize.HasValue);
    }
}

public sealed class GetTasksHandler(IValidator<GetTasksQuery> validator, ITaskRepository repository)
{
    public async Task<IResult> HandleAsync(GetTasksQuery query, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(query, cancellationToken);
        if (validationError is not null)
        {
            return validationError.ToProblem();
        }

        var tasks = await repository.ListAsync(query, cancellationToken);

        return Results.Ok(tasks.Select(TaskDto.From).ToList());
    }
}

public static class GetTasks
{
    public static Task<IResult> HandleAsync(
        int? page,
        int? pageSize,
        GetTasksHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetTasksQuery(page, pageSize), cancellationToken);
}
