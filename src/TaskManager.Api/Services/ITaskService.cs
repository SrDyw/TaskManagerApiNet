using TaskManager.Api.Dtos;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskDto>> GetAllAsync(TaskState? state, string? assignee, CancellationToken cancellationToken);
    Task<TaskDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TaskDto> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken);
    Task<TaskDto?> UpdateAsync(int id, UpdateTaskDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
