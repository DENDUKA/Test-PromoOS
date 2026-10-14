using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TaskApi.Infrastructure.Messaging;

/// <summary>
/// Лениво открывает соединение и канал, переоткрывает их после обрыва и объявляет exchange.
/// Автовосстановление клиента выключено: переподключаемся сами при следующей публикации,
/// так поведение при падении брокера проще объяснить и проверить.
/// </summary>
public sealed class RabbitMqConnectionProvider(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnectionProvider> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;
    private long _retryNotBeforeTickMs;

    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            {
                return _channel;
            }

            if (Environment.TickCount64 < _retryNotBeforeTickMs)
            {
                throw new InvalidOperationException("RabbitMQ is unavailable, reconnect is cooling down.");
            }

            await CloseCoreAsync(TimeSpan.FromSeconds(1));

            try
            {
                await OpenCoreAsync(cancellationToken);
            }
            catch
            {
                _retryNotBeforeTickMs = Environment.TickCount64 + (long)options.Value.ReconnectCooldown.TotalMilliseconds;
                await CloseCoreAsync(TimeSpan.FromSeconds(1));
                throw;
            }

            return _channel!;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Корректно закрывает канал и соединение; безопасно вызывать повторно.</summary>
    public async Task CloseAsync(TimeSpan timeout)
    {
        await _gate.WaitAsync();
        try
        {
            await CloseCoreAsync(timeout);
        }
        finally
        {
            _gate.Release();
        }
    }

    // NOTE: _gate не освобождаем: SemaphoreSlim без AvailableWaitHandle не держит ресурсов, а CloseAsync
    // может быть вызван из StopAsync публикатора уже ПОСЛЕ DisposeAsync (порядок задаёт контейнер DI).
    public async ValueTask DisposeAsync() => await CloseAsync(TimeSpan.FromSeconds(2));

    private async Task OpenCoreAsync(CancellationToken cancellationToken)
    {
        var o = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = o.HostName,
            Port = o.Port,
            UserName = o.UserName,
            Password = o.Password,
            VirtualHost = o.VirtualHost,
            ClientProvidedName = "task-api",
            RequestedConnectionTimeout = o.ConnectionTimeout,
            AutomaticRecoveryEnabled = false,
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);

        // NOTE: канал без publisher confirms — публикация не ждёт подтверждений брокера (осознанно, по ТЗ).
        // HACK: не успел бы надёжно — для гарантий доставки включил бы confirms + transactional outbox.
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            o.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        logger.LogInformation("RabbitMQ connected: {Host}:{Port}, exchange {Exchange}", o.HostName, o.Port, o.Exchange);
    }

    private async Task CloseCoreAsync(TimeSpan timeout)
    {
        var channel = _channel;
        var connection = _connection;
        _channel = null;
        _connection = null;

        using var cts = new CancellationTokenSource(timeout);

        if (channel is not null)
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync(cts.Token);
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to close RabbitMQ channel cleanly");
            }
            finally
            {
                channel.Dispose();
            }
        }

        if (connection is not null)
        {
            try
            {
                if (connection.IsOpen)
                {
                    await connection.CloseAsync(Constants.ReplySuccess, "Application shutdown", timeout, false, cts.Token);
                    logger.LogInformation("RabbitMQ connection closed");
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to close RabbitMQ connection cleanly");
            }
            finally
            {
                connection.Dispose();
            }
        }
    }
}
