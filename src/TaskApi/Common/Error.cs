namespace TaskApi.Common;

public record Error(
    ErrorType Type,
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Failures = null)
{
    public static Error Validation(string code, string message, string field) =>
        new(ErrorType.Validation, code, message, new Dictionary<string, string[]> { [field] = [message] });

    public static Error Validation(string code, IReadOnlyDictionary<string, string[]> failures) =>
        new(ErrorType.Validation, code, "One or more validation errors occurred.", failures);

    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);

    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);
}
