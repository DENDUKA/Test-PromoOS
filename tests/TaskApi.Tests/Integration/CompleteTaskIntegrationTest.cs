using System.Net;
using System.Text.Json;
using TaskApi.Tests.Integration.Infrastructure;

namespace TaskApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class CompleteTaskIntegrationTest(ApiFixture api)
{
    [Fact]
    public async Task CompleteTask_PublishesTaskCompletedMessageToRabbitMq()
    {
        await using var consumer = await TestRabbitConsumer.StartAsync(api.RabbitHost, api.RabbitPort);
        var created = await api.Client.CreateTaskAsync("Buy milk", "High");

        created.IsCompleted.ShouldBeFalse();
        created.CompletedAt.ShouldBeNull();

        var response = await api.Client.CompleteTaskAsync(created.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var completed = await response.ReadAsAsync<TaskResponse>();
        completed.IsCompleted.ShouldBeTrue();
        completed.CompletedAt.ShouldNotBeNull();
        completed.CompletedAt.Value.ShouldBeGreaterThanOrEqualTo(created.CreatedAt);

        var message = await consumer.WaitAsync(m => m.TaskId == created.Id, TimeSpan.FromSeconds(10));
        var received = message.ShouldNotBeNull("RabbitMQ did not receive task.completed within 10 seconds");

        received.RoutingKey.ShouldBe("task.completed");
        received.ContentType.ShouldBe("application/json");
        received.IsPersistent.ShouldBeTrue();

        using var json = JsonDocument.Parse(received.Body);
        var root = json.RootElement;

        root.EnumerateObject().Select(p => p.Name)
            .ShouldBe(["taskId", "title", "completedAt", "priority"], ignoreOrder: true);
        root.GetProperty("taskId").GetGuid().ShouldBe(created.Id);
        root.GetProperty("title").GetString().ShouldBe("Buy milk");
        root.GetProperty("priority").GetString().ShouldBe("High");
        root.GetProperty("completedAt").GetString().ShouldEndWith("Z");
        root.GetProperty("completedAt").GetDateTimeOffset().ShouldBe(completed.CompletedAt.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task CompleteTask_SavesToDatabaseBeforePublishing()
    {
        await using var consumer = await TestRabbitConsumer.StartAsync(api.RabbitHost, api.RabbitPort);
        var unknownId = Guid.NewGuid();

        var response = await api.Client.CompleteTaskAsync(unknownId);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var messages = await consumer.CollectAsync(m => m.TaskId == unknownId, TimeSpan.FromSeconds(1));
        messages.ShouldBeEmpty();
    }
}
