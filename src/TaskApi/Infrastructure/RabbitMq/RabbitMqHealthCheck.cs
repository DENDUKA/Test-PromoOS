using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskApi.Infrastructure.RabbitMq;

public class RabbitMqHealthCheck(RabbitMqConnectionProvider provider) : IHealthCheck
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
