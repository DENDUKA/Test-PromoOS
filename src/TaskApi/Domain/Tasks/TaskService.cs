namespace TaskApi.Domain.Tasks;

public class TaskService(TimeProvider timeProvider)
{
    public TaskItem Create(string title, Priority priority) =>
        TaskItem.Create(title, priority, timeProvider);

    public void Complete(TaskItem task) =>
        task.Complete(timeProvider);
}
