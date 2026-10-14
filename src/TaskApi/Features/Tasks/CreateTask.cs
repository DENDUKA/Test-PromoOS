using FluentValidation;
using TaskApi.Common;
using TaskApi.Domain.Tasks;
using TaskApi.Infrastructure.Database;

namespace TaskApi.Features.Tasks;

public sealed record CreateTaskCommand(string? Title, Priority? Priority);

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(command => command.Title)
            .Cascade(CascadeMode.Stop)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title must not be empty.")
            .Must(title => title!.Trim().Length <= TaskItem.MaxTitleLength)
            .WithMessage($"Title must not exceed {TaskItem.MaxTitleLength} characters.");

        RuleFor(command => command.Priority)
            .IsInEnum()
            .When(command => command.Priority.HasValue);
    }
}

public sealed class CreateTaskHandler(
    IValidator<CreateTaskCommand> validator,
    ITaskRepository repository,
    TimeProvider timeProvider)
{
    public async Task<IResult> HandleAsync(CreateTaskCommand command, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(command, cancellationToken);
        if (validationError is not null)
        {
            return validationError.ToProblem();
        }

        var task = TaskItem.Create(command.Title!.Trim(), command.Priority ?? Priority.Medium, timeProvider);

        await repository.AddAsync(task, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var dto = TaskDto.From(task);

        return Results.Created($"/tasks/{dto.Id}", dto);
    }
}

public static class CreateTask
{
    // Пустое тело → null → валидатор вернёт 400 по Title.
    public static Task<IResult> HandleAsync(
        CreateTaskCommand? command,
        CreateTaskHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(command ?? new CreateTaskCommand(null, null), cancellationToken);
}
