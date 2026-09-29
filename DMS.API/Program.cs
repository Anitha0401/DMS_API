using DMS.API;
using DMS.API.Extension;
using Serilog;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DMS.Data.EF.Context; // for OtherDocumentsContext, GeneralContext
using DMS.Data.EF;
using Microsoft.Extensions.DependencyInjection;
using DMS.Data.EF.Mapping;         // for DBConnectionString

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

// Register DB connection holder used by your DbContexts
services.AddSingleton(new DBConnectionString(configuration.GetConnectionString("DefaultConnection") ?? ""));
 
// existing registrations
services.AddControllers();
services.AddEndpointsApiExplorer();

services.AddServices(configuration);
services.AddDependency(configuration);

builder.AddAssociatedConfiguration();

////builder.Host.ConfigureServices((context, services) =>
////{
////    Log.Logger = new LoggerConfiguration()
////        .ReadFrom.Configuration(context.Configuration)
////        .CreateLogger();
////    services.AddSingleton(Log.Logger);
////});
////builder.Logging.ClearProviders();
////builder.Logging.AddSerilog();

services.AddDistributedMemoryCache();

// Register AutoMapper and scan this project's assembly for profiles
services.AddAutoMapper(cfg =>
{
    // Use a type from the Infrastructure project to get its Assembly
    cfg.AddMaps(typeof(CircularsMappingProfile).Assembly);
});

services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
});


//builder.Services.AddAutoMapper(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
   // app.UseDeveloperExceptionPage();
}
else
{
   // app.UseExceptionHandler("/Error");
}

app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});

//app.MapHealthChecks("/health");
app.UseCors("CorsPolicy");

app.UseHttpLogging();
app.UseHttpsRedirection();

//app.UseRouting();

//app.UseCors("AllowAllOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();
