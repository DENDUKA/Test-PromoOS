namespace TaskApi.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}

public sealed record Error(ErrorType Type, string Code, string Message, string? Field = null)
{
    public static Error Validation(string code, string message, string field) =>
        new(ErrorType.Validation, code, message, field);

    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);

    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);
}

public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, Error? error) : base(error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Result has no value because it is a failure.");

    public static Result<T> Success(T value) => new(value, null);

    public new static Result<T> Failure(Error error) => new(default, error);
}
