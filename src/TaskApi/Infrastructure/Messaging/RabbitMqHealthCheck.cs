using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskApi.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ — не критичная зависимость (задачи завершаются и без него), поэтому недоступность
/// даёт Degraded (HTTP 200), а не Unhealthy: оркестратор не должен перезапускать сервис из-за брокера.
/// </summary>
public sealed class RabbitMqHealthCheck(RabbitMqConnectionProvider provider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await provider.GetChannelAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Degraded("RabbitMQ is unavailable; completion events will be logged and dropped.", exception);
        }
    }
}
