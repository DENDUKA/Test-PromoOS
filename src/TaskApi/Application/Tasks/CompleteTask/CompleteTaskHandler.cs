using TaskApi.Application.Abstractions;
using TaskApi.Application.Common;
using TaskApi.Application.IntegrationEvents;
using TaskApi.Domain.Tasks;
using TaskApi.Domain.Tasks.Events;

namespace TaskApi.Application.Tasks.CompleteTask;

public sealed class CompleteTaskHandler(
    ITaskRepository repository,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher,
    TimeProvider timeProvider)
{
    public async Task<Result<TaskDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await repository.FindByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return Result<TaskDto>.Failure(TaskErrors.NotFound(id));
        }

        try
        {
            task.Complete(timeProvider);
        }
        catch (TaskAlreadyCompletedException)
        {
            return Result<TaskDto>.Failure(TaskErrors.AlreadyCompleted(id));
        }

        try
        {
            // Гонка двух PUT /complete: обе загрузили Version, UPDATE ... WHERE xmin = @old
            // проходит только у первой, у второй будет ConcurrencyConflictException → 409.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<TaskDto>.Failure(TaskErrors.ConcurrentModification(id));
        }

        // NOTE: публикуем строго ПОСЛЕ успешного коммита. Если БД упала — сообщение не уйдёт зря.
        PublishDomainEvents(task);

        return Result<TaskDto>.Success(TaskDto.From(task));
    }

    private void PublishDomainEvents(TaskItem task)
    {
        foreach (var domainEvent in task.DomainEvents.OfType<TaskCompletedDomainEvent>())
        {
            eventPublisher.Publish(new TaskCompletedIntegrationEvent(
                domainEvent.TaskId,
                domainEvent.Title,
                domainEvent.CompletedAt,
                domainEvent.Priority));
        }

        task.ClearDomainEvents();
    }
}
