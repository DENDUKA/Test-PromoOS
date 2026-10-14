using System.Threading.Channels;
using TaskApi.Application.Abstractions;

namespace TaskApi.Infrastructure.Messaging;

/// <summary>
/// Очередь между HTTP-запросами (писатели) и одним фоновым публикатором (читатель).
/// Ограничена по размеру, чтобы лежащий брокер не раздувал память.
/// </summary>
public sealed class IntegrationEventQueue
{
    public const int Capacity = 1000;

    private readonly Channel<IIntegrationEvent> _channel = Channel.CreateBounded<IIntegrationEvent>(
        new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });

    public ChannelReader<IIntegrationEvent> Reader => _channel.Reader;

    public ChannelWriter<IIntegrationEvent> Writer => _channel.Writer;
}
