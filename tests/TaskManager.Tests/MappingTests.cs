using Mapster;
using TaskManager.Api.Dtos;
using TaskManager.Api.Mapping;
using TaskManager.Api.Models;

namespace TaskManager.Tests;

public class MappingTests
{
    private readonly TypeAdapterConfig _config = MappingExtensions.CreateMappingConfig();

    [Fact]
    public void MappingConfig_CompilesWithoutUnmappedMembers()
    {
        var exception = Record.Exception(() => _config.Compile());

        Assert.Null(exception);
    }

    [Fact]
    public void CreateTaskDto_MapsToTaskItem_TrimmingTextAndIgnoringServerFields()
    {
        var dto = new CreateTaskDto
        {
            Title = "  Write report ",
            Description = "   ",
            State = TaskState.InProgress,
            Assignee = " Ana ",
            DueDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var task = dto.Adapt<TaskItem>(_config);

        Assert.Equal(0, task.Id);
        Assert.Equal("Write report", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(TaskState.InProgress, task.State);
        Assert.Equal("Ana", task.Assignee);
        Assert.Equal(dto.DueDate, task.DueDate);
        Assert.Equal(default, task.CreatedAt);
        Assert.Null(task.UpdatedAt);
    }
}
