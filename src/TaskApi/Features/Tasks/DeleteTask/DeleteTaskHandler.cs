using TaskApi.Common;
using TaskApi.Infrastructure.Database;

namespace TaskApi.Features.Tasks.DeleteTask;

public class DeleteTaskHandler(ITaskRepository repository)
{
    public async Task<IResult> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);

        return deleted ? Results.NoContent() : TaskErrors.NotFound(id).ToProblem();
    }
}
