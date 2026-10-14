using TaskApi.Features.Tasks.CompleteTask;
using TaskApi.Features.Tasks.CreateTask;
using TaskApi.Features.Tasks.DeleteTask;
using TaskApi.Features.Tasks.GetTasks;

namespace TaskApi.Features.Tasks;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var tasks = app.MapGroup("/tasks");

        tasks.MapPost("/", (CreateTaskCommand? command, CreateTaskHandler handler, CancellationToken cancellationToken) =>
            handler.HandleAsync(command ?? new CreateTaskCommand(null, null), cancellationToken));

        tasks.MapGet("/", (int? page, int? pageSize, GetTasksHandler handler, CancellationToken cancellationToken) =>
            handler.HandleAsync(new GetTasksQuery(page, pageSize), cancellationToken));

        tasks.MapPut("/{id:guid}/complete", (Guid id, CompleteTaskHandler handler, CancellationToken cancellationToken) =>
            handler.HandleAsync(id, cancellationToken));

        tasks.MapDelete("/{id:guid}", (Guid id, DeleteTaskHandler handler, CancellationToken cancellationToken) =>
            handler.HandleAsync(id, cancellationToken));

        return app;
    }
}
