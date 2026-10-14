using FluentValidation;
using FluentValidation.Results;

namespace TaskApi.Common;

public static class ValidationExtensions
{
    public const string ValidationFailedCode = "validation.failed";

    public static async Task<Error?> ValidateToErrorAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);

        return result.IsValid ? null : result.ToError();
    }

    public static Error ToError(this ValidationResult result)
    {
        var failures = result.Errors
            .GroupBy(failure => ToCamelCase(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return Error.Validation(ValidationFailedCode, failures);
    }

    private static string ToCamelCase(string propertyName) =>
        propertyName.Length == 0 ? propertyName : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}
