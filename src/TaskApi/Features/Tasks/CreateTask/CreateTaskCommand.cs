using TaskApi.Domain.Tasks;

namespace TaskApi.Features.Tasks.CreateTask;

public record CreateTaskCommand(string? Title, Priority? Priority);
