using System.Net;
using System.Text.Json;
using TaskApi.Tests.Integration.Infrastructure;

namespace TaskApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class TasksCrudTests(ApiFixture api) : IAsyncLifetime
{
    public Task InitializeAsync() => api.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_Returns201WithLocationHeader()
    {
        var response = await api.Client.PostRawAsync("{\"title\":\"Buy milk\"}");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var task = await response.ReadAsAsync<TaskResponse>();
        response.Headers.Location!.OriginalString.ShouldBe($"/tasks/{task.Id}");
    }

    [Fact]
    public async Task GetAll_WithoutParameters_ReturnsAllTasksWithoutPaging()
    {
        for (var i = 0; i < 55; i++)
        {
            await api.Client.CreateTaskAsync($"Task {i}");
        }

        var response = await api.Client.GetAsync("/tasks");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadAsAsync<List<TaskResponse>>()).Count.ShouldBe(55);
    }

    [Fact]
    public async Task GetAll_WithPaging_ReturnsRequestedPage()
    {
        for (var i = 0; i < 5; i++)
        {
            await api.Client.CreateTaskAsync($"Task {i}");
        }

        var response = await api.Client.GetAsync("/tasks?page=3&pageSize=2");

        (await response.ReadAsAsync<List<TaskResponse>>()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAll_ReturnsTasksInCreationOrder()
    {
        await api.Client.CreateTaskAsync("first");
        await api.Client.CreateTaskAsync("second");
        await api.Client.CreateTaskAsync("third");

        var tasks = await (await api.Client.GetAsync("/tasks")).ReadAsAsync<List<TaskResponse>>();

        tasks.Select(t => t.Title).ShouldBe(["first", "second", "third"]);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=201")]
    [InlineData("page=abc")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=1000")]
    public async Task GetAll_WithInvalidPaging_Returns400(string query)
    {
        var response = await api.Client.GetAsync($"/tasks?{query}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_WithSeveralInvalidParameters_ReportsAllOfThem()
    {
        var response = await api.Client.GetAsync("/tasks?page=0&pageSize=1000");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().ShouldBe("validation.failed");

        var errors = problem.RootElement.GetProperty("errors");
        errors.TryGetProperty("page", out _).ShouldBeTrue();
        errors.TryGetProperty("pageSize", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task GetAll_WithMaxPageSize_Returns200()
    {
        await api.Client.CreateTaskAsync("Only one");

        var response = await api.Client.GetAsync("/tasks?page=1&pageSize=200");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadAsAsync<List<TaskResponse>>()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAll_PageBeyondLast_ReturnsEmptyList()
    {
        await api.Client.CreateTaskAsync("Only one");

        var response = await api.Client.GetAsync("/tasks?page=5&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadAsAsync<List<TaskResponse>>()).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAll_WithPaging_PagesDoNotOverlap()
    {
        for (var i = 0; i < 5; i++)
        {
            await api.Client.CreateTaskAsync($"Task {i}");
        }

        var first = await (await api.Client.GetAsync("/tasks?page=1&pageSize=3")).ReadAsAsync<List<TaskResponse>>();
        var second = await (await api.Client.GetAsync("/tasks?page=2&pageSize=3")).ReadAsAsync<List<TaskResponse>>();

        first.Select(t => t.Title).ShouldBe(["Task 0", "Task 1", "Task 2"]);
        second.Select(t => t.Title).ShouldBe(["Task 3", "Task 4"]);
    }

    [Fact]
    public async Task Create_PersistsTaskWithGeneratedId()
    {
        var first = await api.Client.CreateTaskAsync("First", "Low");
        var second = await api.Client.CreateTaskAsync("Second");

        first.Id.ShouldNotBe(Guid.Empty);
        second.Id.ShouldNotBe(first.Id);

        var stored = await api.Client.GetTaskAsync(first.Id);
        stored.Title.ShouldBe("First");
        stored.Priority.ShouldBe("Low");
        stored.IsCompleted.ShouldBeFalse();
        stored.CompletedAt.ShouldBeNull();
        stored.CreatedAt.ShouldBe(first.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Complete_PersistsAllFieldsAndKeepsCreatedAt()
    {
        var created = await api.Client.CreateTaskAsync("Persist me", "High");
        await Task.Delay(50);

        var completed = await (await api.Client.CompleteTaskAsync(created.Id)).ReadAsAsync<TaskResponse>();
        var stored = await api.Client.GetTaskAsync(created.Id);

        completed.CompletedAt!.Value.ShouldBeGreaterThan(created.CreatedAt);
        stored.Id.ShouldBe(created.Id);
        stored.Title.ShouldBe("Persist me");
        stored.Priority.ShouldBe("High");
        stored.IsCompleted.ShouldBeTrue();
        stored.CreatedAt.ShouldBe(created.CreatedAt, TimeSpan.FromMilliseconds(1));
        stored.CompletedAt!.Value.ShouldBe(completed.CompletedAt.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Complete_DeletedTask_Returns404()
    {
        var created = await api.Client.CreateTaskAsync("Deleted before completion");
        await api.Client.DeleteAsync($"/tasks/{created.Id}");

        var response = await api.Client.CompleteTaskAsync(created.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Delete_UnknownTask_Returns404()
    {
        var response = await api.Client.DeleteAsync($"/tasks/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_CompletedTask_Returns204()
    {
        var created = await api.Client.CreateTaskAsync("Done and deleted");
        await api.Client.CompleteTaskAsync(created.Id);

        var response = await api.Client.DeleteAsync($"/tasks/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Complete_UnknownTask_Returns404()
    {
        var response = await api.Client.CompleteTaskAsync(Guid.NewGuid());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Complete_WithInvalidGuid_Returns404()
    {
        var response = await api.Client.PutAsync("/tasks/not-a-guid/complete", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ExistingTask_Returns204_ThenTaskIsGone()
    {
        var created = await api.Client.CreateTaskAsync("Delete me");

        var deleted = await api.Client.DeleteAsync($"/tasks/{created.Id}");
        var again = await api.Client.DeleteAsync($"/tasks/{created.Id}");

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        again.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var tasks = await (await api.Client.GetAsync("/tasks")).ReadAsAsync<List<TaskResponse>>();
        tasks.ShouldNotContain(t => t.Id == created.Id);
    }
}
