using DMS.Application;
using DMS.Application.Interface;
using DMS.Data.EF.Features;
using DMS.Data.EF.Query.Circulars;
using DMS.Data.EF.Query.Manuals;
using DMS.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DMS.Data.EF;

public static class ConfigrationRepositorys
{
    public static IServiceCollection DataEFDependencyRegister(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddScoped<ManualGeneralQueries>();
        service.AddScoped<CIRGeneralQueries>();
        service.AddScoped<OtherDocumentGeneralQueries>();
        service.BaseDBDependencyRegister(configuration);

        return service;
    }

    public static IServiceCollection BaseDBDependencyRegister(this IServiceCollection service, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("ConnectionStrings").GetSection("LocalDBConnection").Value;

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
        }

        service.AddScoped<IDBActions>(sp => new DBActions(connectionString));

        return service;
    }

    public static IServiceCollection RepositorysDependencyRegister(this IServiceCollection service)
    {
        service.AddScoped<IManualService, ManualService>();
        service.AddScoped<ICIRService, CIRService>();
        service.AddScoped<IOtherDocumentService, OtherDocumentService>();
        service.AddScoped<IVesselService, VesselService>();
        service.AddScoped<ISyncService, SyncService>();
        service.AddScoped<IHQUserService, HQUserService>();
        service.AddScoped<ILoginService, LoginService>();

        // New features (Database/012_NewFeatures.sql)
        service.AddScoped<SqlDb>();
        service.AddScoped<IAckDashboardService, AckDashboardService>();
        service.AddScoped<ISuggestionService, SuggestionService>();
        service.AddScoped<ISearchService, SearchService>();
        return service;
    }
}
