using TaskApi.Common;
using TaskApi.Domain.Tasks;
using TaskApi.Infrastructure.Database;
using TaskApi.Infrastructure.RabbitMq;

namespace TaskApi.Features.Tasks.CompleteTask;

public class CompleteTaskHandler(
    ITaskRepository repository,
    IIntegrationEventPublisher eventPublisher,
    TaskService taskService)
{
    public async Task<IResult> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await repository.FindByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return TaskErrors.NotFound(id).ToProblem();
        }

        try
        {
            taskService.Complete(task);
        }
        catch (TaskAlreadyCompletedException)
        {
            return TaskErrors.AlreadyCompleted(id).ToProblem();
        }

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return TaskErrors.ConcurrentModification(id).ToProblem();
        }

        eventPublisher.Publish(new TaskCompletedIntegrationEvent(task.Id, task.Title, task.CompletedAt!.Value, task.Priority));

        return Results.Ok(TaskDto.From(task));
    }
}
