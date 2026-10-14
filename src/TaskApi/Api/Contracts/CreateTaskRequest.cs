using TaskApi.Domain.Tasks;

namespace TaskApi.Api.Contracts;

/// <summary>Priority необязателен: по умолчанию Medium. Неизвестное значение отсекается десериализатором (400).</summary>
public sealed record CreateTaskRequest(string? Title, Priority? Priority);
