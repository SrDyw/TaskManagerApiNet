using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Dtos;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public class TaskService(AppDbContext db, IMapper mapper, TimeProvider timeProvider) : ITaskService
{
    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(TaskState? state, string? assignee, CancellationToken cancellationToken)
    {
        var query = db.Tasks.AsNoTracking();

        if (state.HasValue)
        {
            query = query.Where(t => t.State == state.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignee))
        {
            var normalized = assignee.Trim();
            query = query.Where(t => t.Assignee == normalized);
        }

        var tasks = await query
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<TaskDto>>(tasks);
    }

    public async Task<TaskDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return task is null ? null : mapper.Map<TaskDto>(task);
    }

    public async Task<TaskDto> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken)
    {
        var task = mapper.Map<TaskItem>(dto);
        task.CreatedAt = timeProvider.GetUtcNow().UtcDateTime;

        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        return mapper.Map<TaskDto>(task);
    }

    public async Task<TaskDto?> UpdateAsync(int id, UpdateTaskDto dto, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return null;
        }

        mapper.Map(dto, task);
        task.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        await db.SaveChangesAsync(cancellationToken);

        return mapper.Map<TaskDto>(task);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return false;
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
