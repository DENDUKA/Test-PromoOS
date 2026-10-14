using FluentValidation;
using TaskApi.Common;
using TaskApi.Domain.Tasks;
using TaskApi.Infrastructure.Database;

namespace TaskApi.Features.Tasks.CreateTask;

public class CreateTaskHandler(
    IValidator<CreateTaskCommand> validator,
    ITaskRepository repository,
    TaskService taskService)
{
    public async Task<IResult> HandleAsync(CreateTaskCommand command, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(command, cancellationToken);
        if (validationError is not null)
        {
            return validationError.ToProblem();
        }

        var task = taskService.Create(command.Title!.Trim(), command.Priority ?? Priority.Medium);

        await repository.AddAsync(task, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var dto = TaskDto.From(task);

        return Results.Created($"/tasks/{dto.Id}", dto);
    }
}
