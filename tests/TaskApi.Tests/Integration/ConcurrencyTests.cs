using System.Net;
using TaskApi.Tests.Integration.Infrastructure;

namespace TaskApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ConcurrencyTests(ApiFixture api)
{
    [Fact]
    public async Task CompleteTask_WhenCalledConcurrently_OneRequestWinsAndOthersGet409()
    {
        await using var consumer = await TestRabbitConsumer.StartAsync(api.RabbitHost, api.RabbitPort);
        var created = await api.Client.CreateTaskAsync("Race me");

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => api.Client.CompleteTaskAsync(created.Id)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(4);
        responses
            .Where(r => r.StatusCode == HttpStatusCode.Conflict)
            .ShouldAllBe(r => r.Content.Headers.ContentType!.MediaType == "application/problem+json");

        var delivered = await consumer.CollectAsync(m => m.TaskId == created.Id, TimeSpan.FromSeconds(3));
        delivered.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CompleteTask_WhenAlreadyCompleted_Returns409AndKeepsOriginalCompletedAt()
    {
        var created = await api.Client.CreateTaskAsync("Complete me twice");
        var first = await api.Client.CompleteTaskAsync(created.Id);
        var firstTask = await first.ReadAsAsync<TaskResponse>();

        var second = await api.Client.CompleteTaskAsync(created.Id);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var stored = await api.Client.GetTaskAsync(created.Id);
        stored.CompletedAt!.Value.ShouldBe(firstTask.CompletedAt!.Value, TimeSpan.FromMilliseconds(1));
    }
}
