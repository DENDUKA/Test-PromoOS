using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskApi.Infrastructure.Persistence;

/// <summary>Только для `dotnet ef migrations ...`: чтобы не поднимать приложение целиком.</summary>
public sealed class TaskDbContextFactory : IDesignTimeDbContextFactory<TaskDbContext>
{
    public TaskDbContext CreateDbContext(string[] args)
    {
        // Для генерации миграций подключение к БД не нужно, поэтому пароля здесь нет.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=taskapi;Username=postgres";

        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TaskDbContext(options);
    }
}
