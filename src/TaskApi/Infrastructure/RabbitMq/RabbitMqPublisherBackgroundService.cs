using System.Threading.Channels;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TaskApi.Infrastructure.RabbitMq;

public class RabbitMqPublisherBackgroundService(
    Channel<IIntegrationEvent> channel,
    RabbitMqConnectionProvider connection,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqPublisherBackgroundService> logger) : BackgroundService
{
    private readonly CancellationTokenSource _abortPublishing = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var integrationEvent in channel.Reader.ReadAllAsync(CancellationToken.None))
        {
            if (_abortPublishing.IsCancellationRequested)
            {
                LogUnsent(integrationEvent, "shutdown timeout elapsed");
                continue;
            }

            await PublishAsync(integrationEvent, _abortPublishing.Token);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        channel.Writer.TryComplete();

        try
        {
            await DrainAsync(cancellationToken);

            await _abortPublishing.CancelAsync();

            await base.StopAsync(cancellationToken);
        }
        finally
        {
            await connection.CloseAsync(TimeSpan.FromSeconds(2));
        }
    }

    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        var drainTimeout = options.Value.ShutdownDrainTimeout;

        try
        {
            await (ExecuteTask ?? Task.CompletedTask).WaitAsync(drainTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Publisher did not drain within {Timeout}; aborting remaining publishes", drainTimeout);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Host shutdown token fired before publisher drained; aborting remaining publishes");
        }
    }

    private async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        try
        {
            var body = MessagingJson.ToUtf8Bytes(integrationEvent);

            var rabbitChannel = await connection.GetChannelAsync(cancellationToken);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = integrationEvent.EventId.ToString(),
                Type = integrationEvent.RoutingKey,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            };

            await rabbitChannel.BasicPublishAsync(
                options.Value.Exchange,
                integrationEvent.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogDebug("Published {RoutingKey} ({EventId})", integrationEvent.RoutingKey, integrationEvent.EventId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogUnsent(integrationEvent, "publish aborted on shutdown");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to publish {RoutingKey} ({EventId}); task stays completed. Payload: {Payload}",
                integrationEvent.RoutingKey,
                integrationEvent.EventId,
                MessagingJson.ToJson(integrationEvent));
        }
    }

    private void LogUnsent(IIntegrationEvent integrationEvent, string reason) =>
        logger.LogError(
            "Event {RoutingKey} ({EventId}) was NOT sent: {Reason}. Payload: {Payload}",
            integrationEvent.RoutingKey,
            integrationEvent.EventId,
            reason,
            MessagingJson.ToJson(integrationEvent));
}
