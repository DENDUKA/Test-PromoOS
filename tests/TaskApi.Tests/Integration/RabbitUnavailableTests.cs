using System.Net;
using Microsoft.Extensions.Logging;
using TaskApi.Tests.Integration.Infrastructure;

namespace TaskApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class RabbitUnavailableTests(ApiFixture api)
{
    private const string DeadHost = "127.0.0.1";
    private const int DeadPort = 1;

    [Fact]
    public async Task CompleteTask_WhenRabbitMqIsDown_StillCompletesTaskAndLogsPayload()
    {
        await using var factory = api.CreateFactory(DeadHost, DeadPort, rabbitConnectTimeoutSeconds: 1);
        using var client = factory.CreateClient();
        var created = await client.CreateTaskAsync("Offline task", "High");

        var response = await client.CompleteTaskAsync(created.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = await client.GetTaskAsync(created.Id);
        stored.IsCompleted.ShouldBeTrue();
        stored.CompletedAt.ShouldNotBeNull();

        var logged = await TestWait.UntilAsync(
            () => api.Logs.Entries.Any(e =>
                e.Level == LogLevel.Error
                && e.Message.Contains("Failed to publish", StringComparison.Ordinal)
                && e.Message.Contains(created.Id.ToString(), StringComparison.Ordinal)),
            TimeSpan.FromSeconds(10));
        logged.ShouldBeTrue("expected an error log with the unsent task.completed payload");
    }

    [Fact]
    public async Task Health_WhenRabbitMqIsDown_ReportsDegradedButStaysAvailable()
    {
        await using var factory = api.CreateFactory(DeadHost, DeadPort, rabbitConnectTimeoutSeconds: 1);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Degraded");
    }

    [Fact]
    public async Task Health_WhenEverythingIsUp_ReportsHealthy()
    {
        var response = await api.Client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }
}
