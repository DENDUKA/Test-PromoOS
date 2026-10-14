using System.Threading.Channels;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TaskApi.Infrastructure.Messaging;

/// <summary>
/// Единственный читатель очереди событий: читает их и публикует в RabbitMQ.
/// Любая ошибка публикации логируется и НЕ пробрасывается — иначе упавший брокер остановил бы хост.
/// </summary>
public sealed class RabbitMqPublisherBackgroundService(
    Channel<IIntegrationEvent> channel,
    RabbitMqConnectionProvider connection,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqPublisherBackgroundService> logger) : BackgroundService
{
    private readonly CancellationTokenSource _abortPublishing = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // NOTE: stoppingToken здесь намеренно НЕ используем. Цикл завершается, когда очередь закрыта
        // (Writer.TryComplete в StopAsync) и вычитана до конца, — так уже начатые публикации успевают уйти.
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
        // 1. Новые события больше не принимаются (ChannelEventPublisher залогирует их как не поставленные).
        channel.Writer.TryComplete();

        try
        {
            // 2. Даём фоновому циклу ShutdownDrainTimeout, чтобы отправить то, что уже в очереди.
            await DrainAsync(cancellationToken);

            // 3. Всё, что не успело, прерываем: остаток цикл залогирует (LogUnsent) и выйдет.
            await _abortPublishing.CancelAsync();

            await base.StopAsync(cancellationToken);
        }
        finally
        {
            // 4. Соединение закрываем в любом случае, даже если цикл завершился ошибкой.
            await connection.CloseAsync(TimeSpan.FromSeconds(2));
        }
    }

    // NOTE: _abortPublishing намеренно не освобождается (нет Dispose): у источника без таймеров и подписок нет
    // ресурсов, а контейнер DI вызывает Dispose() сервиса независимо от порядка относительно StopAsync —
    // освобождённый источник ронял бы остановку хоста с ObjectDisposedException.

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
            // Fail silently (по ТЗ), но с логом и полным payload — чтобы событие можно было переслать руками.
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
