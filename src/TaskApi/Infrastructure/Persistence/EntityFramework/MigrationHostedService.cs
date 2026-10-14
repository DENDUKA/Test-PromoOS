using Microsoft.EntityFrameworkCore;

namespace TaskApi.Infrastructure.Persistence.EntityFramework;

// HACK: миграции применяются при старте приложения, чтобы `docker compose up` работал из коробки.
// В проде гонял бы их отдельным шагом деплоя (init-контейнер / dotnet ef database update).
public class MigrationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<MigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
