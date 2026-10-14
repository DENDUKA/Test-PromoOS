using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TaskApi.Tests.Integration.Infrastructure;

public class TestRabbitConsumer : IAsyncDisposable
{
    public const string Exchange = "task.events";

    private readonly Channel<ReceivedMessage> _messages = Channel.CreateUnbounded<ReceivedMessage>();
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private TestRabbitConsumer(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<TestRabbitConsumer> StartAsync(string host, int port, string routingKey = "task.completed")
    {
        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = ApiFixture.RabbitUser,
            Password = ApiFixture.RabbitPassword,
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        var consumer = new TestRabbitConsumer(connection, channel);

        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        var queue = await channel.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(queue.QueueName, Exchange, routingKey);

        var asyncConsumer = new AsyncEventingBasicConsumer(channel);
        asyncConsumer.ReceivedAsync += (_, args) =>
        {
            consumer._messages.Writer.TryWrite(new ReceivedMessage(
                args.RoutingKey,
                Encoding.UTF8.GetString(args.Body.Span),
                args.BasicProperties.ContentType,
                args.BasicProperties.MessageId,
                args.BasicProperties.DeliveryMode == DeliveryModes.Persistent));
            return Task.CompletedTask;
        };
        await channel.BasicConsumeAsync(queue.QueueName, autoAck: true, asyncConsumer);

        return consumer;
    }

    public async Task<ReceivedMessage?> WaitAsync(Func<ReceivedMessage, bool> predicate, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            await foreach (var message in _messages.Reader.ReadAllAsync(cts.Token))
            {
                if (predicate(message))
                {
                    return message;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        return null;
    }

    public async Task<IReadOnlyList<ReceivedMessage>> CollectAsync(Func<ReceivedMessage, bool> predicate, TimeSpan window)
    {
        var collected = new List<ReceivedMessage>();
        using var cts = new CancellationTokenSource(window);

        try
        {
            await foreach (var message in _messages.Reader.ReadAllAsync(cts.Token))
            {
                if (predicate(message))
                {
                    collected.Add(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        return collected;
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
        _channel.Dispose();
        _connection.Dispose();
    }
}
