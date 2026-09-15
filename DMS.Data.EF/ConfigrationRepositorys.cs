using DMS.Data.EF.Query.Circulars;
using DMS.Data.EF.Query.Manuals;
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
}
