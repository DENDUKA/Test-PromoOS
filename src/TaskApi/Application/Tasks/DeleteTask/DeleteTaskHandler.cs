using TaskApi.Application.Abstractions;
using TaskApi.Application.Common;

namespace TaskApi.Application.Tasks.DeleteTask;

public sealed class DeleteTaskHandler(ITaskRepository repository)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);

        return deleted ? Result.Success() : Result.Failure(TaskErrors.NotFound(id));
    }
}
