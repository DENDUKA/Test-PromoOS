namespace TaskApi.Application.Abstractions;

public interface IUnitOfWork
{
    /// <exception cref="ConcurrencyConflictException">Строку изменил или удалил другой запрос.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Application-представление DbUpdateConcurrencyException, чтобы слой не зависел от EF Core.</summary>
public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("The record was modified by another request.", innerException);
