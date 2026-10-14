using System.Text.Json.Serialization;
using TaskApi.Common;
using TaskApi.Domain.Tasks;
using TaskApi.Infrastructure.Database;
using TaskApi.Infrastructure.Messaging;

namespace TaskApi.Features.Tasks;

/// <summary>Контракт сообщения для внешних потребителей: ровно {taskId, title, completedAt, priority}.</summary>
public sealed record TaskCompletedIntegrationEvent(
    Guid TaskId,
    string Title,
    DateTimeOffset CompletedAt,
    Priority Priority) : IIntegrationEvent
{
    public const string Key = "task.completed";

    [JsonIgnore]
    public Guid EventId { get; } = Guid.NewGuid();

    [JsonIgnore]
    public string RoutingKey => Key;
}

public sealed class CompleteTaskHandler(
    ITaskRepository repository,
    IIntegrationEventPublisher eventPublisher,
    TimeProvider timeProvider)
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
            task.Complete(timeProvider);
        }
        catch (TaskAlreadyCompletedException)
        {
            return TaskErrors.AlreadyCompleted(id).ToProblem();
        }

        try
        {
            // Гонка двух PUT /complete: обе загрузили Version, UPDATE ... WHERE xmin = @old
            // проходит только у первой, у второй будет ConcurrencyConflictException → 409.
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return TaskErrors.ConcurrentModification(id).ToProblem();
        }

        // NOTE: публикуем строго ПОСЛЕ успешного коммита. Если БД упала — сообщение не уйдёт зря.
        eventPublisher.Publish(new TaskCompletedIntegrationEvent(task.Id, task.Title, task.CompletedAt!.Value, task.Priority));

        return Results.Ok(TaskDto.From(task));
    }
}

public static class CompleteTask
{
    public static Task<IResult> HandleAsync(
        Guid id,
        CompleteTaskHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(id, cancellationToken);
}
