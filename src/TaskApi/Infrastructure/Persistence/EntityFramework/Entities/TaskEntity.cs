using TaskApi.Domain.Tasks;

namespace TaskApi.Infrastructure.Persistence.EntityFramework.Entities;

public class TaskEntity
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public bool IsCompleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public Priority Priority { get; set; }

    public uint Version { get; set; }
}
