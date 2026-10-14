using TaskApi.Application.Abstractions;
using TaskApi.Application.Common;
using TaskApi.Domain.Tasks;

namespace TaskApi.Application.Tasks.CreateTask;

public sealed record CreateTaskCommand(string? Title, Priority? Priority);

public sealed class CreateTaskHandler(
    ITaskRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<TaskDto>> HandleAsync(CreateTaskCommand command, CancellationToken cancellationToken)
    {
        if (!TaskTitle.TryCreate(command.Title, out var title, out var error))
        {
            return Result<TaskDto>.Failure(Error.Validation("task.invalid_title", error, "title"));
        }

        var task = TaskItem.Create(title, command.Priority ?? Priority.Medium, timeProvider);

        await repository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TaskDto>.Success(TaskDto.From(task));
    }
}
