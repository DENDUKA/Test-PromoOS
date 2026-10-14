namespace TaskApi.Common;

public static class TaskErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("task.not_found", $"Task '{id}' was not found.");

    public static Error AlreadyCompleted(Guid id) =>
        Error.Conflict("task.already_completed", $"Task '{id}' is already completed.");

    public static Error ConcurrentModification(Guid id) =>
        Error.Conflict("task.concurrent_modification", $"Task '{id}' was modified by another request. Retry.");
}
