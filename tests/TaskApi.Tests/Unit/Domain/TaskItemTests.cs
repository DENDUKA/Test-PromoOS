using Microsoft.Extensions.Time.Testing;
using TaskApi.Domain.Tasks;

namespace TaskApi.Tests.Unit.Domain;

public class TaskItemTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CompletedAt = new(2026, 5, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(CreatedAt);

    private TaskItem NewTask(Priority priority = Priority.Medium)
    {
        return TaskItem.Create("Buy milk", priority, _time);
    }

    [Fact]
    public void Create_SetsDefaults_AndLeavesCompletedAtEmpty()
    {
        var task = NewTask();

        task.Id.ShouldBe(Guid.Empty);
        task.IsCompleted.ShouldBeFalse();
        task.CompletedAt.ShouldBeNull();
        task.Priority.ShouldBe(Priority.Medium);
        task.CreatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public void AssignId_SetsId_AndThrowsIfReassigned()
    {
        var task = NewTask();
        var id = Guid.NewGuid();

        task.AssignId(id);
        task.Id.ShouldBe(id);

        Should.Throw<InvalidOperationException>(() => task.AssignId(Guid.NewGuid()));
    }

    [Fact]
    public void Complete_SetsCompletedAtToCompletionMoment_NotCreationMoment()
    {
        var task = NewTask();
        _time.SetUtcNow(CompletedAt);

        task.Complete(_time);

        task.IsCompleted.ShouldBeTrue();
        task.CompletedAt.ShouldBe(CompletedAt);
        task.CreatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public void Complete_Throws_WhenTaskIsAlreadyCompleted_AndKeepsFirstCompletedAt()
    {
        var task = NewTask();
        _time.SetUtcNow(CompletedAt);
        task.Complete(_time);
        _time.SetUtcNow(CompletedAt.AddHours(1));

        Should.Throw<TaskAlreadyCompletedException>(() => task.Complete(_time));
        task.CompletedAt.ShouldBe(CompletedAt);
    }
}
