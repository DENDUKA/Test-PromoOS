using FluentValidation;
using TaskApi.Domain.Tasks;

namespace TaskApi.Features.Tasks.CreateTask;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
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
