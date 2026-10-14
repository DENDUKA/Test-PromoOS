using System.Text.Json.Serialization;
using TaskApi.Application.Abstractions;
using TaskApi.Domain.Tasks;

namespace TaskApi.Application.IntegrationEvents;

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
