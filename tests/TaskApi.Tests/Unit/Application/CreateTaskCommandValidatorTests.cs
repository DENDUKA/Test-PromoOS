using FluentValidation.TestHelper;
using TaskApi.Domain.Tasks;
using TaskApi.Features.Tasks;

namespace TaskApi.Tests.Unit.Application;

public sealed class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Title_EmptyOrWhitespace_IsRejected(string? title)
    {
        var result = _validator.TestValidate(new CreateTaskCommand(title, null));

        result.ShouldHaveValidationErrorFor(command => command.Title)
            .WithErrorMessage("Title must not be empty.");
    }

    [Fact]
    public void Title_LongerThanMax_IsRejected()
    {
        var title = new string('a', TaskItem.MaxTitleLength + 1);

        var result = _validator.TestValidate(new CreateTaskCommand(title, null));

        result.ShouldHaveValidationErrorFor(command => command.Title);
    }

    [Fact]
    public void Title_ExactlyMax_IsAccepted()
    {
        var title = new string('a', TaskItem.MaxTitleLength);

        var result = _validator.TestValidate(new CreateTaskCommand(title, null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Title_PaddedWithSpaces_IsMeasuredAfterTrim()
    {
        var title = $"  {new string('a', TaskItem.MaxTitleLength)}  ";

        var result = _validator.TestValidate(new CreateTaskCommand(title, null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Priority_UnknownEnumValue_IsRejected()
    {
        var result = _validator.TestValidate(new CreateTaskCommand("Buy milk", (Priority)42));

        result.ShouldHaveValidationErrorFor(command => command.Priority);
    }

    [Fact]
    public void ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreateTaskCommand("Buy milk", Priority.High));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
