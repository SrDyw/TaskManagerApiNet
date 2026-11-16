using TaskManager.Api.Models;

namespace TaskManager.Api.Dtos;

public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskState State { get; set; } = TaskState.Pending;
    public string Assignee { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}
