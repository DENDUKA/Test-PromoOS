namespace TaskApi.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// HACK: миграции применяются при старте приложения, чтобы `docker compose up` работал из коробки.
    /// В проде я бы гонял их отдельным шагом деплоя (init-контейнер / dotnet ef database update).
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; init; } = true;
}
