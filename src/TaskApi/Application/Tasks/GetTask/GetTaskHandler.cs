using TaskApi.Application.Abstractions;
using TaskApi.Application.Common;

namespace TaskApi.Application.Tasks.GetTask;

public sealed class GetTaskHandler(ITaskRepository repository)
{
    public async Task<Result<TaskDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await repository.FindByIdAsync(id, cancellationToken);

        return task is null
            ? Result<TaskDto>.Failure(TaskErrors.NotFound(id))
            : Result<TaskDto>.Success(TaskDto.From(task));
    }
}
