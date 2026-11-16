using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Dtos;
using TaskManager.Api.Models;
using TaskManager.Tests.Infrastructure;

namespace TaskManager.Tests;

public class TasksApiTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private const string BaseUrl = "/api/tasks";
    private readonly HttpClient _client = factory.CreateClient();

    private static CreateTaskDto ValidCreateDto() => new()
    {
        Title = "Prepare sprint demo",
        Description = "Slides and live walkthrough",
        Assignee = "Ana",
        DueDate = DateTime.UtcNow.AddDays(7)
    };

    private async Task<TaskDto> CreateTaskAsync(CreateTaskDto? dto = null)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, dto ?? ValidCreateDto(), TaskManagerApiFactory.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskDto>(TaskManagerApiFactory.JsonOptions))!;
    }

    private async Task<ValidationProblemDetails> ReadProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TaskManagerApiFactory.JsonOptions))!;

    // ---------- Create ----------

    [Fact]
    public async Task Create_WithValidPayload_Returns201AndPersistsMappedTask()
    {
        var dto = ValidCreateDto();
        dto.Title = "  Prepare sprint demo  ";
        dto.Assignee = " Ana ";

        var response = await _client.PostAsJsonAsync(BaseUrl, dto, TaskManagerApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskDto>(TaskManagerApiFactory.JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Prepare sprint demo", created.Title);
        Assert.Equal("Ana", created.Assignee);
        Assert.Equal(TaskState.Pending, created.State);
        Assert.Null(created.UpdatedAt);
        Assert.Equal($"{BaseUrl}/{created.Id}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        var fetched = await _client.GetFromJsonAsync<TaskDto>($"{BaseUrl}/{created.Id}", TaskManagerApiFactory.JsonOptions);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Create_WithMissingFieldsAndPastDueDate_Returns400WithFieldErrors()
    {
        var dto = new CreateTaskDto
        {
            Title = "",
            Assignee = "",
            DueDate = DateTime.UtcNow.AddDays(-1)
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, dto, TaskManagerApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Contains("Title", problem.Errors.Keys);
        Assert.Contains("Assignee", problem.Errors.Keys);
        Assert.Contains("DueDate", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithTitleTooLongAndUnknownState_Returns400()
    {
        var dto = ValidCreateDto();
        dto.Title = new string('x', 151);
        dto.State = (TaskState)99;

        var response = await _client.PostAsJsonAsync(BaseUrl, dto, TaskManagerApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Contains("Title", problem.Errors.Keys);
        Assert.Contains("State", problem.Errors.Keys);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_ExistingTask_Returns200AndPersistsChanges()
    {
        var created = await CreateTaskAsync();
        var update = new UpdateTaskDto
        {
            Title = "Prepare sprint demo v2",
            Description = null,
            State = TaskState.InProgress,
            Assignee = "Luis",
            DueDate = created.DueDate.AddDays(1)
        };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", update, TaskManagerApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await _client.GetFromJsonAsync<TaskDto>($"{BaseUrl}/{created.Id}", TaskManagerApiFactory.JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Prepare sprint demo v2", updated.Title);
        Assert.Null(updated.Description);
        Assert.Equal(TaskState.InProgress, updated.State);
        Assert.Equal("Luis", updated.Assignee);
        Assert.Equal(update.DueDate, updated.DueDate);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task Update_WithInvalidPayload_Returns400AndLeavesTaskUnchanged()
    {
        var created = await CreateTaskAsync();
        var update = new UpdateTaskDto
        {
            Title = "",
            State = TaskState.Done,
            Assignee = "Luis",
            DueDate = created.DueDate
        };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", update, TaskManagerApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Title", (await ReadProblemAsync(response)).Errors.Keys);

        var unchanged = await _client.GetFromJsonAsync<TaskDto>($"{BaseUrl}/{created.Id}", TaskManagerApiFactory.JsonOptions);
        Assert.Equal(created, unchanged);
    }

    [Fact]
    public async Task Update_NonExistingTask_Returns404()
    {
        var update = new UpdateTaskDto
        {
            Title = "Ghost task",
            State = TaskState.Pending,
            Assignee = "Nobody",
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/999999", update, TaskManagerApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_ExistingTask_Returns204AndTaskIsGone()
    {
        var created = await CreateTaskAsync();

        var response = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await _client.GetAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingTask_Returns404()
    {
        var response = await _client.DeleteAsync($"{BaseUrl}/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SameTaskTwice_SecondCallReturns404()
    {
        var created = await CreateTaskAsync();

        var first = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");
        var second = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }
}
