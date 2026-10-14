using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace TaskApi.Infrastructure.Persistence;

/// <summary>
/// Применяет миграции до старта Kestrel: хостед-сервисы запускаются раньше веб-сервера,
/// поэтому первый запрос никогда не увидит схему без таблиц.
/// </summary>
public sealed class MigrationHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<DatabaseOptions> options,
    ILogger<MigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup)
        {
            logger.LogInformation("Applying migrations on startup is disabled");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
