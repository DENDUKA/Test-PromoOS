namespace TaskApi.Domain.Tasks;

public class TaskAlreadyCompletedException(Guid taskId)
    : Exception($"Task '{taskId}' is already completed.")
{
    public Guid TaskId { get; } = taskId;
}
