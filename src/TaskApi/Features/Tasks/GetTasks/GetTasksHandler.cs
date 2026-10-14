using FluentValidation;
using TaskApi.Common;
using TaskApi.Infrastructure.Database;

namespace TaskApi.Features.Tasks.GetTasks;

public class GetTasksHandler(IValidator<GetTasksQuery> validator, ITaskRepository repository)
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
