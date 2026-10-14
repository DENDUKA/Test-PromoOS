using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskApi.Tests.Integration.Infrastructure;

public static class TaskApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<TaskResponse> CreateTaskAsync(this HttpClient client, string title, string? priority = null)
    {
        var response = await client.PostAsJsonAsync("/tasks", new { title, priority }, Json);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
    }

    public static Task<HttpResponseMessage> CompleteTaskAsync(this HttpClient client, Guid id) =>
        client.PutAsync($"/tasks/{id}/complete", content: null);

    public static async Task<TaskResponse> GetTaskAsync(this HttpClient client, Guid id)
    {
        var tasks = await (await client.GetAsync("/tasks")).ReadAsAsync<List<TaskResponse>>();

        return tasks.Single(task => task.Id == id);
    }

    public static Task<HttpResponseMessage> PostRawAsync(this HttpClient client, string json) =>
        client.PostAsync("/tasks", new StringContent(json, Encoding.UTF8, "application/json"));

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;
}
