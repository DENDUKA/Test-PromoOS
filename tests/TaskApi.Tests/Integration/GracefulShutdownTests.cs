using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskApi.Domain.Tasks;
using TaskApi.Features.Tasks.CompleteTask;
using TaskApi.Infrastructure.RabbitMq;
using TaskApi.Tests.Integration.Infrastructure;

namespace TaskApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class GracefulShutdownTests(ApiFixture api)
{
    [Fact]
    public async Task StopAsync_DeliversAlreadyQueuedEvents_BeforeClosingConnection()
    {
        var options = Options.Create(new RabbitMqOptions
        {
            HostName = api.RabbitHost,
            Port = api.RabbitPort,
            UserName = ApiFixture.RabbitUser,
            Password = ApiFixture.RabbitPassword,
        });
        var channel = Channel.CreateBounded<IIntegrationEvent>(1000);
        await using var connection = new RabbitMqConnectionProvider(options, NullLogger<RabbitMqConnectionProvider>.Instance);
        using var service = new RabbitMqPublisherBackgroundService(
            channel, connection, options, NullLogger<RabbitMqPublisherBackgroundService>.Instance);
        var publisher = new ChannelEventPublisher(channel, NullLogger<ChannelEventPublisher>.Instance);
        await using var consumer = await TestRabbitConsumer.StartAsync(api.RabbitHost, api.RabbitPort);

        await service.StartAsync(CancellationToken.None);
        var ids = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        foreach (var id in ids)
        {
            publisher.Publish(new TaskCompletedIntegrationEvent(id, "queued", DateTimeOffset.UtcNow, Priority.Low));
        }

        await service.StopAsync(CancellationToken.None);

        var delivered = await consumer.CollectAsync(m => ids.Contains(m.TaskId), TimeSpan.FromSeconds(3));
        delivered.Count.ShouldBe(ids.Count);
    }

    [Fact]
    public async Task StopAsync_WhenBrokerIsDown_FinishesQuicklyAndLogsUnsentEvents()
    {
        var options = Options.Create(new RabbitMqOptions
        {
            HostName = "127.0.0.1",
            Port = 1,
            UserName = ApiFixture.RabbitUser,
            Password = ApiFixture.RabbitPassword,
            ConnectionTimeoutSeconds = 1,
            ShutdownDrainTimeoutSeconds = 5,
        });
        var logs = new InMemoryLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        var channel = Channel.CreateBounded<IIntegrationEvent>(1000);
        await using var connection = new RabbitMqConnectionProvider(options, loggerFactory.CreateLogger<RabbitMqConnectionProvider>());
        using var service = new RabbitMqPublisherBackgroundService(
            channel, connection, options, loggerFactory.CreateLogger<RabbitMqPublisherBackgroundService>());
        var publisher = new ChannelEventPublisher(channel, loggerFactory.CreateLogger<ChannelEventPublisher>());

        await service.StartAsync(CancellationToken.None);
        var queuedId = Guid.NewGuid();
        publisher.Publish(new TaskCompletedIntegrationEvent(queuedId, "queued", DateTimeOffset.UtcNow, Priority.High));

        var stopwatch = Stopwatch.StartNew();
        await service.StopAsync(CancellationToken.None);
        stopwatch.Stop();

        var lateId = Guid.NewGuid();
        publisher.Publish(new TaskCompletedIntegrationEvent(lateId, "late", DateTimeOffset.UtcNow, Priority.High));

        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(8));
        logs.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains(queuedId.ToString(), StringComparison.Ordinal));
        logs.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains(lateId.ToString(), StringComparison.Ordinal));
    }
}
