using Mapster;
using TaskManager.Api.Dtos;
using TaskManager.Api.Models;

namespace TaskManager.Api.Mapping;

public class TaskMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TaskItem, TaskDto>();

        config.NewConfig<CreateTaskDto, TaskItem>()
            .Map(dest => dest.Title, src => src.Title.Trim())
            .Map(dest => dest.Description, src => string.IsNullOrWhiteSpace(src.Description) ? null! : src.Description.Trim())
            .Map(dest => dest.Assignee, src => src.Assignee.Trim())
            .Ignore(dest => dest.Id, dest => dest.CreatedAt, dest => dest.UpdatedAt);

        config.NewConfig<UpdateTaskDto, TaskItem>()
            .Map(dest => dest.Title, src => src.Title.Trim())
            .Map(dest => dest.Description, src => string.IsNullOrWhiteSpace(src.Description) ? null! : src.Description.Trim())
            .Map(dest => dest.Assignee, src => src.Assignee.Trim())
            .Ignore(dest => dest.Id, dest => dest.CreatedAt, dest => dest.UpdatedAt);
    }
}
