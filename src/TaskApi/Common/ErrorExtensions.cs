namespace TaskApi.Common;

public static class ErrorExtensions
{
    public static IResult ToProblem(this Error error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        return error.Type switch
        {
            ErrorType.Validation => Results.ValidationProblem(
                new Dictionary<string, string[]>(error.Failures ?? new Dictionary<string, string[]> { ["request"] = [error.Message] }),
                extensions: extensions),
            ErrorType.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: error.Message,
                extensions: extensions),
            ErrorType.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: error.Message,
                extensions: extensions),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }
}
