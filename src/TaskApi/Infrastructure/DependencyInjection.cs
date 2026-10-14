using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskApi.Infrastructure.Database;
using TaskApi.Infrastructure.Persistence.EntityFramework;
using TaskApi.Infrastructure.RabbitMq;

namespace TaskApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddPersistence();
        services.AddRabbitMq();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services)
    {
        services.AddDbContext<TaskDbContext>((serviceProvider, options) =>
            options.UseNpgsql(GetConnectionString(serviceProvider)));

        services.AddScoped<ITaskRepository, TaskRepository>();

        services.AddHostedService<MigrationHostedService>();

        services.AddHealthChecks().AddNpgSql(GetConnectionString, name: "postgres");
    }

    private static void AddRabbitMq(this IServiceCollection services)
    {
        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(Channel.CreateBounded<IIntegrationEvent>(
            new BoundedChannelOptions(1000) { SingleReader = true }));
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IIntegrationEventPublisher, ChannelEventPublisher>();
        services.AddHostedService<RabbitMqPublisherBackgroundService>();

        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", HealthStatus.Degraded);
    }

    private static string GetConnectionString(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Default")
            is { Length: > 0 } connectionString
            ? connectionString
            : throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
}
