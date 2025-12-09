using DMS.Application;
using DMS.Data.EF;

namespace DMS.API;

public static class DependencyRegistration
{
    public static IServiceCollection AddDependency(this IServiceCollection services, IConfiguration configuration)
    {
        // services.DataEFDependencyRegister(configuration);
        services.DataEFDependencyRegister(configuration);

        services.RepositorysDependencyRegister();

        return services;
    }
}
