using FluentValidation.TestHelper;
using TaskApi.Features.Tasks;

namespace TaskApi.Tests.Unit.Application;

public sealed class GetTasksQueryValidatorTests
{
    private readonly GetTasksQueryValidator _validator = new();

    [Fact]
    public void NoPagingParameters_IsAccepted()
    {
        var result = _validator.TestValidate(new GetTasksQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_BelowOne_IsRejected(int page)
    {
        var result = _validator.TestValidate(new GetTasksQuery(Page: page));

        result.ShouldHaveValidationErrorFor(query => query.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetTasksQuery.MaxPageSize + 1)]
    public void PageSize_OutOfRange_IsRejected(int pageSize)
    {
        var result = _validator.TestValidate(new GetTasksQuery(PageSize: pageSize));

        result.ShouldHaveValidationErrorFor(query => query.PageSize);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, GetTasksQuery.MaxPageSize)]
    public void PageAndPageSize_InsideRange_AreAccepted(int page, int pageSize)
    {
        var result = _validator.TestValidate(new GetTasksQuery(Page: page, PageSize: pageSize));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void BothPagingParameters_Invalid_ReportsBothFields()
    {
        var result = _validator.TestValidate(new GetTasksQuery(Page: 0, PageSize: 0));

        result.ShouldHaveValidationErrorFor(query => query.Page);
        result.ShouldHaveValidationErrorFor(query => query.PageSize);
    }
}
