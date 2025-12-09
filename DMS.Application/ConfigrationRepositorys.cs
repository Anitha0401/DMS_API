using DMS.Application.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace DMS.Application;

public static class ConfigrationRepositorys
{
    public static IServiceCollection RepositorysDependencyRegister(this IServiceCollection service)
    {
        service.AddScoped<IDMSService, DMSService>();
        service.AddScoped<ICIRService, CIRService>();
        service.AddScoped<IVesselService, VesselService>();
        service.AddScoped<IHQUserService, HQUserService>();
        service.AddScoped<ILoginService, LoginService>();
        return service;
    }
}
