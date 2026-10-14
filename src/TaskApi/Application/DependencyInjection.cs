using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskApi.Application.Tasks.CompleteTask;
using TaskApi.Application.Tasks.CreateTask;
using TaskApi.Application.Tasks.DeleteTask;
using TaskApi.Application.Tasks.GetTask;
using TaskApi.Application.Tasks.GetTasks;

namespace TaskApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<CreateTaskHandler>();
        services.AddScoped<CompleteTaskHandler>();
        services.AddScoped<DeleteTaskHandler>();
        services.AddScoped<GetTasksHandler>();
        services.AddScoped<GetTaskHandler>();

        return services;
    }
}
