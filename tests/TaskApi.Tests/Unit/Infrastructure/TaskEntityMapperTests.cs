using Microsoft.Extensions.Time.Testing;
using TaskApi.Domain.Tasks;
using TaskApi.Infrastructure.Persistence.EntityFramework.Entities;

namespace TaskApi.Tests.Unit.Infrastructure;

public class TaskEntityMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(Now);

    [Fact]
    public void ToEntity_ThenToDomain_PreservesAllFields()
    {
        var task = TaskItem.Create("Buy milk", Priority.High, _time);
        task.Complete(_time);

        var restored = task.ToEntity().ToDomain();

        restored.ShouldBe(task);
    }

    [Fact]
    public void ToDomain_RestoresStateWithoutCompletingAnything()
    {
        var entity = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = "Stored",
            IsCompleted = false,
            CreatedAt = Now,
            CompletedAt = null,
            Priority = Priority.Low,
            Version = 42,
        };

        var task = entity.ToDomain();

        task.Id.ShouldBe(entity.Id);
        task.IsCompleted.ShouldBeFalse();
        task.CompletedAt.ShouldBeNull();
        task.Priority.ShouldBe(Priority.Low);
    }

    [Fact]
    public void CopyFrom_TransfersDomainChangesButKeepsVersion()
    {
        var task = TaskItem.Create("Buy milk", Priority.Medium, _time);
        var entity = task.ToEntity();
        entity.Version = 7;
        _time.Advance(TimeSpan.FromHours(2));
        task.Complete(_time);

        entity.CopyFrom(task);

        entity.IsCompleted.ShouldBeTrue();
        entity.CompletedAt.ShouldBe(task.CompletedAt);
        entity.Version.ShouldBe(7u);
    }
}
