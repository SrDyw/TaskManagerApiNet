using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Dtos;
using TaskManager.Api.Models;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController(ITaskService taskService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetAll(
        [FromQuery] TaskState? state,
        [FromQuery] string? assignee,
        CancellationToken cancellationToken)
    {
        var tasks = await taskService.GetAllAsync(state, assignee, cancellationToken);
        return Ok(tasks);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var task = await taskService.GetByIdAsync(id, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost]
    [ProducesResponseType<TaskDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Create(CreateTaskDto dto, CancellationToken cancellationToken)
    {
        var task = await taskService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Update(int id, UpdateTaskDto dto, CancellationToken cancellationToken)
    {
        var task = await taskService.UpdateAsync(id, dto, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await taskService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
