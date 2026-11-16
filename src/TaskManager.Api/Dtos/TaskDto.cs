using TaskManager.Api.Models;

namespace TaskManager.Api.Dtos;

public record TaskDto(
    int Id,
    string Title,
    string? Description,
    TaskState State,
    string Assignee,
    DateTime DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
