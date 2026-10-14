using TaskApi.Domain.Tasks;

namespace TaskApi.Tests.Unit.Domain;

public class TaskTitleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void TryCreate_ReturnsFalse_WhenTitleIsNullEmptyOrWhitespace(string? raw)
    {
        var created = TaskTitle.TryCreate(raw, out var title, out var error);

        created.ShouldBeFalse();
        title.ShouldBeNull();
        error.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TryCreate_ReturnsFalse_WhenTitleIsLongerThan200()
    {
        var created = TaskTitle.TryCreate(new string('a', TaskTitle.MaxLength + 1), out _, out var error);

        created.ShouldBeFalse();
        error!.ShouldContain("200");
    }

    [Fact]
    public void TryCreate_ReturnsTrue_WhenTitleIsExactly200()
    {
        var created = TaskTitle.TryCreate(new string('a', TaskTitle.MaxLength), out var title, out _);

        created.ShouldBeTrue();
        title!.Value.Length.ShouldBe(200);
    }

    [Fact]
    public void TryCreate_TrimsSurroundingWhitespace()
    {
        var created = TaskTitle.TryCreate("  Buy milk  ", out var title, out _);

        created.ShouldBeTrue();
        title!.Value.ShouldBe("Buy milk");
    }

    [Fact]
    public void TryCreate_ChecksLengthAfterTrim()
    {
        var raw = "  " + new string('a', TaskTitle.MaxLength) + "  ";

        TaskTitle.TryCreate(raw, out _, out _).ShouldBeTrue();
    }
}
