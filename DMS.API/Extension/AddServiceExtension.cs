using DMS.Data.EF;
using DMS.Data.EF.Context;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using System.Net.Mime;
using System.Reflection;

namespace DMS.API.Extension;

public static class AddServiceExtension
{
    public static WebApplicationBuilder AddAssociatedConfiguration(this WebApplicationBuilder builder)
    {
        // Set the base path for configuration files
        builder.Configuration.SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Assembly location is null."));
        // Load configuration files
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        // Configure logging provider
        builder.Logging.ClearProviders(); // Optional: Clears default loggingproviders
        builder.Logging.AddConsole(); // Adds console logging provider
        builder.Logging.AddDebug(); // Adds Debug logging provider Set specific log levels for different librarues
        builder.Logging.SetMinimumLevel(LogLevel.Information); // Default minimum level
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning); // For Microsoft.* namespaces
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning); // For ASP.NET Core
        builder.Logging.AddFilter("System", LogLevel.Warning); // For System.* namespaces
        builder.Logging.AddFilter("YourApp.Namespace", LogLevel.Trace); // For your custom namespace

        return builder;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
        services.AddHttpClient();

        AddSwagger(services);
        AddDBContext(services, configuration);
        AddJWTToken(services, configuration);
        AddLogging(services);
        AddCORS(services, configuration);

        return services;
    }

    public static void AddSwagger(IServiceCollection services)
    {
        services.AddSession();
        
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        //services.AddSwaggerGen();

        //Add authentication in Swagger
        services.AddSwaggerGen(opt => {
            opt.SwaggerDoc("v1", new OpenApiInfo { Title = "DMS", Version = "v1" });
            opt.AddServer(new OpenApiServer { Url = "https://localhost:7151" }); // or your actual URL
            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { In = ParameterLocation.Header, Description = "Please enter token", Name = "Authorization", Type = SecuritySchemeType.Http, BearerFormat = "JWT", Scheme = "bearer" });
            opt.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
        });

    }

    public static void AddDBContext(IServiceCollection services, IConfiguration configuration)
    {
        var configValue = configuration.GetValue<string>("ConnectionStrings:LocalDBConnection");

        if (string.IsNullOrEmpty(configValue))
        {
            throw new InvalidOperationException("The connection string 'ConnectionStrings:LocalDBConnection' is not configured or is null/empty.");
        }

        services.AddDbContext<DmsContext>(options =>
            options.UseSqlServer(configValue));

        services.AddDbContext<CircularsContext>(options =>
            options.UseSqlServer(configValue));

        services.AddDbContext<GeneralContext>(options =>
            options.UseSqlServer(configValue));

        services.AddDbContext<OtherDocumentsContext>(options =>
            options.UseSqlServer(configValue));

        DBConnectionString dBConnectionStringOption = new(configValue);
        dBConnectionStringOption.DefaultConnection = configValue;
        services.AddSingleton(dBConnectionStringOption);
    }

    public static void AddJWTToken(IServiceCollection services, IConfiguration configuration)
    {
        var initialScope = configuration.GetValue<string>("AzureAd:Scope")?.Split(' ');
        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
           .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"))
           .EnableTokenAcquisitionToCallDownstreamApi(initialScope)
              .AddInMemoryTokenCaches();

        //JWT Authentication
        //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //    .AddJwtBearer(options => {
        //    options.RequireHttpsMetadata = false;
        //    options.SaveToken = true;
        //    options.TokenValidationParameters = new TokenValidationParameters
        //    {
        //        ValidateIssuer = true,
        //        ValidateAudience = true,
        //        ValidateLifetime = true,
        //        ValidateIssuerSigningKey = true,
        //        ValidIssuer = configuration["Jwt:Issuer"],
        //        ValidAudience = configuration["Jwt:Audience"],
        //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
        //    };
        //})

        ////Add authentication in Swagger
        //services.AddSwaggerGen(opt => {
        //    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "API Sample", Version = "v1" });
        //    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { In = ParameterLocation.Header, Description = "Please enter token", Name = "Authorization", Type = SecuritySchemeType.Http, BearerFormat = "JWT", Scheme = "bearer" });
        //    opt.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
        //});

    }

    public static void AddLogging(IServiceCollection services)
    {
        services.AddLogging(configure => {
            configure.AddConsole();       // Log to the console
            configure.SetMinimumLevel(LogLevel.Information); // Minimum level
        });

        services.AddHttpLogging(configure =>
        {
            configure.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod
                                      | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
                                      | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestQuery
                                      | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode;
            configure.RequestHeaders.Add(HeaderNames.Referer);
            configure.RequestHeaders.Add(HeaderNames.Authorization); // Log Authorization header
            configure.MediaTypeOptions.AddText(MediaTypeNames.Application.Json);
            configure.RequestBodyLogLimit = 4096; // Limit request body size to 4KB
            configure.ResponseBodyLogLimit = 4096; // Limit response body size to 4KB   
            configure.RequestHeaders.Add("Authorization"); // Log Authorization header
            configure.ResponseHeaders.Add("X-Response-Time"); // Log custom response header
        });
    }

    public static void AddCORS(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedCorsOrigin = configuration
            .GetSection("AllowedCorsOrigin")
            .AsEnumerable()
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .Select(x => x.Value)
            .ToArray();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins",
                policy =>
                {
                    policy.AllowAnyOrigin()  // WithOrigins(allowedCorsOrigin)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
        });
    }

    public static bool IsDebugInfoAllowed(this IWebHostEnvironment env)
    {
        string[] debugEnvSuffix = { "DV", "DEV", "QA", "Local" };
        return env.IsDevelopment() || debugEnvSuffix.Any(suffix => env.EnvironmentName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }
}
