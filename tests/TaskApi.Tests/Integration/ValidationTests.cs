using System.Net;
using System.Text.Json;
using TaskApi.Tests.Integration.Infrastructure;

namespace TaskApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ValidationTests(ApiFixture api)
{
    [Theory]
    [InlineData("{\"title\":\"\"}")]
    [InlineData("{\"title\":\"   \"}")]
    [InlineData("{\"title\":null}")]
    [InlineData("{}")]
    [InlineData("{\"title\":\"\\t\\n\"}")]
    public async Task Create_WhenTitleIsEmptyOrMissing_Returns400WithTitleError(string body)
    {
        var response = await api.Client.PostRawAsync(body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").TryGetProperty("title", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Create_WhenTitleIsLongerThan200_Returns400()
    {
        var response = await api.Client.PostRawAsync($"{{\"title\":\"{new string('a', 201)}\"}}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WhenTitleIsExactly200_Returns201()
    {
        var task = await api.Client.CreateTaskAsync(new string('a', 200));

        task.Title.Length.ShouldBe(200);
    }

    [Fact]
    public async Task Create_WhenTitleIs200AfterTrim_Returns201()
    {
        var task = await api.Client.CreateTaskAsync($"  {new string('a', 200)}  ");

        task.Title.Length.ShouldBe(200);
    }

    [Fact]
    public async Task Create_WhenTitleIsInvalid_ReturnsCamelCaseFieldAndMessage()
    {
        var response = await api.Client.PostRawAsync("{\"title\":\"\"}");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.RootElement.GetProperty("errors").EnumerateObject().Select(p => p.Name).ShouldBe(["title"]);
        problem.RootElement.GetProperty("errors").GetProperty("title")[0].GetString().ShouldBe("Title must not be empty.");
    }

    [Fact]
    public async Task Create_TrimsTitle()
    {
        var task = await api.Client.CreateTaskAsync("  Buy milk  ");

        task.Title.ShouldBe("Buy milk");
    }

    [Fact]
    public async Task Create_WithoutBody_Returns400()
    {
        var response = await api.Client.PostAsync("/tasks", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithMalformedJson_Returns400()
    {
        var response = await api.Client.PostRawAsync("{\"title\": ");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("\"Urgent\"")]
    [InlineData("99")]
    [InlineData("1")]
    public async Task Create_WithUnknownPriority_Returns400(string priorityJson)
    {
        var response = await api.Client.PostRawAsync($"{{\"title\":\"x\",\"priority\":{priorityJson}}}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithoutPriority_UsesDefaults()
    {
        var task = await api.Client.CreateTaskAsync("Defaults");

        task.Priority.ShouldBe("Medium");
        task.IsCompleted.ShouldBeFalse();
        task.CompletedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    public async Task Create_WithValidPriority_StoresIt(string priority)
    {
        var task = await api.Client.CreateTaskAsync("With priority", priority);

        task.Priority.ShouldBe(priority);
    }
}
