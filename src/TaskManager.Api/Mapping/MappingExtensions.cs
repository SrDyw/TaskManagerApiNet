using Mapster;
using MapsterMapper;

namespace TaskManager.Api.Mapping;

public static class MappingExtensions
{
    public static TypeAdapterConfig CreateMappingConfig()
    {
        var config = new TypeAdapterConfig();
        config.RequireExplicitMapping = true;
        config.RequireDestinationMemberSource = true;
        config.Scan(typeof(TaskMappingConfig).Assembly);
        return config;
    }

    public static IServiceCollection AddMappings(this IServiceCollection services)
    {
        var config = CreateMappingConfig();
        config.Compile();

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        return services;
    }
}
