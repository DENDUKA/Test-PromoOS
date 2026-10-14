using TaskApi.Common;
using TaskApi.Infrastructure.Database;

namespace TaskApi.Features.Tasks;

public sealed class DeleteTaskHandler(ITaskRepository repository)
{
    public async Task<IResult> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);

        return deleted ? Results.NoContent() : TaskErrors.NotFound(id).ToProblem();
    }
}

public static class DeleteTask
{
    public static Task<IResult> HandleAsync(
        Guid id,
        DeleteTaskHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(id, cancellationToken);
}
