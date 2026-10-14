using FluentValidation;

namespace TaskApi.Features.Tasks.GetTasks;

public class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
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
