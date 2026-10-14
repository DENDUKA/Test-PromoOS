using System.Globalization;
using TaskApi.Api.Contracts;
using TaskApi.Api.Errors;
using TaskApi.Application.Tasks.CompleteTask;
using TaskApi.Application.Tasks.CreateTask;
using TaskApi.Application.Tasks.DeleteTask;
using TaskApi.Application.Tasks.GetTask;
using TaskApi.Application.Tasks.GetTasks;

namespace TaskApi.Api.Endpoints;

public static class TaskEndpoints
{
    public const string TotalCountHeader = "X-Total-Count";

    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // {id:guid} — невалидный guid в маршруте даёт 404 ещё до хендлера, лишней валидации не нужно.
        var tasks = app.MapGroup("/tasks");

        tasks.MapPost("/", CreateAsync);
        tasks.MapGet("/", GetAllAsync);
        tasks.MapGet("/{id:guid}", GetByIdAsync);
        tasks.MapPut("/{id:guid}/complete", CompleteAsync);
        tasks.MapDelete("/{id:guid}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateTaskRequest? request,
        CreateTaskHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateTaskCommand(request?.Title, request?.Priority), cancellationToken);

        return result.Match(task => Results.Created($"/tasks/{task.Id}", task));
    }

    /// <summary>Без параметров возвращает ВСЕ задачи (по заданию). page/pageSize/фильтр/сортировка — бонус.</summary>
    private static async Task<IResult> GetAllAsync(
        int? page,
        int? pageSize,
        bool? isCompleted,
        TaskSortBy? sortBy,
        bool? desc,
        GetTasksHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var query = new GetTasksQuery(page, pageSize, isCompleted, sortBy ?? TaskSortBy.CreatedAt, desc ?? false);
        var result = await handler.HandleAsync(query, cancellationToken);

        return result.Match(list =>
        {
            response.Headers[TotalCountHeader] = list.TotalCount.ToString(CultureInfo.InvariantCulture);
            return Results.Ok(list.Items);
        });
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        GetTaskHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Match(Results.Ok);
    }

    private static async Task<IResult> CompleteAsync(
        Guid id,
        CompleteTaskHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Match(Results.Ok);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        DeleteTaskHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Match(Results.NoContent);
    }
}
