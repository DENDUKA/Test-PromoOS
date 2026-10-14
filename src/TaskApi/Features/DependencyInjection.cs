using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskApi.Domain.Tasks;
using TaskApi.Features.Tasks.CompleteTask;
using TaskApi.Features.Tasks.CreateTask;
using TaskApi.Features.Tasks.DeleteTask;
using TaskApi.Features.Tasks.GetTasks;

namespace TaskApi.Features;

public static class DependencyInjection
{
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<TaskService>();

        services.AddValidatorsFromAssemblyContaining<CreateTaskCommandValidator>(ServiceLifetime.Singleton);

        services.AddScoped<CreateTaskHandler>();
        services.AddScoped<CompleteTaskHandler>();
        services.AddScoped<DeleteTaskHandler>();
        services.AddScoped<GetTasksHandler>();

        return services;
    }
}
