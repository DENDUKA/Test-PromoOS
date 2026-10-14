namespace TaskApi.Domain.Tasks;

public record TaskItem
{
    public const int MaxTitleLength = 200;

    private TaskItem()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = null!;

    public bool IsCompleted { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public Priority Priority { get; private set; }

    public static TaskItem Restore(
        Guid id,
        string title,
        bool isCompleted,
        DateTimeOffset createdAt,
        DateTimeOffset? completedAt,
        Priority priority) => new()
    {
        Id = id,
        Title = title,
        IsCompleted = isCompleted,
        CreatedAt = createdAt,
        CompletedAt = completedAt,
        Priority = priority,
    };

    public static TaskItem Create(string title, Priority priority, TimeProvider timeProvider) => new()
    {
        Id = Guid.Empty,
        Title = title,
        Priority = priority,
        IsCompleted = false,
        CreatedAt = timeProvider.GetUtcNow(),
        CompletedAt = null,
    };

    public void AssignId(Guid id)
    {
        if (Id != Guid.Empty && Id != id)
        {
            throw new InvalidOperationException("Task ID has already been assigned.");
        }

        Id = id;
    }

    public void Complete(TimeProvider timeProvider)
    {
        if (IsCompleted)
        {
            throw new TaskAlreadyCompletedException(Id);
        }

        IsCompleted = true;
        CompletedAt = timeProvider.GetUtcNow();
    }
}
