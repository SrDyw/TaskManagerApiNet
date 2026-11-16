using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Api.Dtos;
using TaskManager.Api.Models;
using TaskManager.Api.Services;
using TaskManager.Tests.Infrastructure;

namespace TaskManager.Tests;

public class ErrorHandlingTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private const string BaseUrl = "/api/tasks";
    private const string ProblemJson = "application/problem+json";
    private readonly HttpClient _client = factory.CreateClient();

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static string FutureDate() => DateTime.UtcNow.AddDays(3).ToString("O");

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    // ---------- Malformed requests ----------

    [Theory]
    [InlineData("{\"title\": \"x\",")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    public async Task Post_WithMalformedOrMissingBody_Returns400(string body)
    {
        var response = await _client.PostAsync(BaseUrl, Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_WithUnsupportedContentType_Returns415()
    {
        var content = new StringContent("{\"title\":\"x\"}", Encoding.UTF8, "text/plain");

        var response = await _client.PostAsync(BaseUrl, content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Theory]
    [InlineData("{\"title\":null,\"assignee\":\"Ana\",\"dueDate\":\"{0}\"}", "Title")]
    [InlineData("{\"title\":\"x\",\"assignee\":\"Ana\",\"state\":\"Archived\",\"dueDate\":\"{0}\"}", "state")]
    [InlineData("{\"title\":\"x\",\"assignee\":\"Ana\",\"dueDate\":\"not-a-date\"}", "dueDate")]
    [InlineData("{\"title\":\"x\",\"assignee\":\"Ana\",\"dueDate\":null}", "dueDate")]
    public async Task Post_WithInvalidFieldValues_Returns400NamingTheField(string template, string field)
    {
        var response = await _client.PostAsync(BaseUrl, Json(template.Replace("{0}", FutureDate())));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problem!.Errors.Keys, key => key.Contains(field, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Get_WithInvalidStateFilter_Returns400()
    {
        var response = await _client.GetAsync($"{BaseUrl}?state=Archived");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Consistent ProblemDetails bodies ----------

    [Fact]
    public async Task ValidationErrors_UseStandardProblemDetailsShapeInEnglish()
    {
        var dto = new CreateTaskDto
        {
            Title = new string('x', 151),
            Assignee = "Ana",
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, dto, TaskManagerApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("type", out _));
        Assert.True(body.TryGetProperty("traceId", out _));
        var message = body.GetProperty("errors").GetProperty("Title")[0].GetString();
        Assert.Contains("150 characters or fewer", message);
    }

    [Theory]
    [InlineData("GET", "/api/tasks/999999", HttpStatusCode.NotFound)]
    [InlineData("DELETE", "/api/tasks/999999", HttpStatusCode.NotFound)]
    [InlineData("GET", "/api/tasks/abc", HttpStatusCode.NotFound)]
    [InlineData("GET", "/api/tasks/99999999999", HttpStatusCode.NotFound)]
    [InlineData("GET", "/api/unknown", HttpStatusCode.NotFound)]
    [InlineData("PATCH", "/api/tasks/1", HttpStatusCode.MethodNotAllowed)]
    public async Task NotFoundAndRoutingErrors_ReturnProblemDetailsBody(string method, string url, HttpStatusCode expected)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "PATCH" ? Json("{}") : null
        });

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)expected, (await ReadJsonAsync(response)).GetProperty("status").GetInt32());
    }

    // ---------- Dates ----------

    [Fact]
    public async Task Post_WithDueDateWithoutOffset_IsTreatedAsUtcConsistently()
    {
        var response = await _client.PostAsync(BaseUrl,
            Json("{\"title\":\"x\",\"assignee\":\"Ana\",\"dueDate\":\"2030-06-01T10:00:00\"}"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskDto>(TaskManagerApiFactory.JsonOptions);
        var expected = new DateTime(2030, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, created!.DueDate);
        Assert.Equal(DateTimeKind.Utc, created.DueDate.Kind);

        var fetched = await _client.GetFromJsonAsync<TaskDto>($"{BaseUrl}/{created.Id}", TaskManagerApiFactory.JsonOptions);
        Assert.Equal(expected, fetched!.DueDate);
    }

    // ---------- Unexpected failures ----------

    private HttpClient CreateClientWithFailingService(Exception exception) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<ITaskService>(_ => new FailingTaskService(exception)))).CreateClient();

    [Fact]
    public async Task UnhandledException_Returns500WithoutLeakingDetails_AndApiKeepsServing()
    {
        var client = CreateClientWithFailingService(new InvalidOperationException("Database connection string leaked here"));

        var failed = await client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        Assert.Equal(ProblemJson, failed.Content.Headers.ContentType?.MediaType);
        var raw = await failed.Content.ReadAsStringAsync();
        Assert.DoesNotContain("leaked", raw);
        Assert.DoesNotContain("InvalidOperationException", raw);
        Assert.Equal(500, (await ReadJsonAsync(failed)).GetProperty("status").GetInt32());

        // The host is still alive and the next request is served normally.
        var next = await client.GetAsync(BaseUrl);
        Assert.Equal(HttpStatusCode.InternalServerError, next.StatusCode);
        var healthy = await _client.GetAsync(BaseUrl);
        Assert.Equal(HttpStatusCode.OK, healthy.StatusCode);
    }

    [Fact]
    public async Task ConcurrencyConflict_Returns409()
    {
        var client = CreateClientWithFailingService(new DbUpdateConcurrencyException("Row was deleted."));
        var update = new UpdateTaskDto
        {
            Title = "x",
            Assignee = "Ana",
            State = TaskState.Done,
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        var response = await client.PutAsJsonAsync($"{BaseUrl}/1", update, TaskManagerApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    private sealed class FailingTaskService(Exception exception) : ITaskService
    {
        public Task<IReadOnlyList<TaskDto>> GetAllAsync(TaskState? state, string? assignee, CancellationToken cancellationToken) => throw exception;
        public Task<TaskDto?> GetByIdAsync(int id, CancellationToken cancellationToken) => throw exception;
        public Task<TaskDto> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken) => throw exception;
        public Task<TaskDto?> UpdateAsync(int id, UpdateTaskDto dto, CancellationToken cancellationToken) => throw exception;
        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) => throw exception;
    }
}
