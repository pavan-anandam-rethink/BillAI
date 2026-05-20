using HealthChecks.UI.Client;
using IdentityService.Api.Middleware;
using IdentityService.Application;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Configurations;
using IdentityService.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "IdentityService")
        .WriteTo.Console();
});

// Configuration
var config = builder.Configuration;
var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
config.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services - Clean Architecture layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(config);
builder.Services.AddPersistenceServices(config);

// JWT Authentication
builder.Services.AddJwtAuthentication(config);

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Identity Service API",
        Version = "v1",
        Description = "Enterprise Identity Service - Clean Architecture"
    });
    options.SwaggerDoc("legacy", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Identity Service API (Legacy)",
        Version = "legacy",
        Description = "Legacy SSO endpoints for backward compatibility"
    });

    // JWT Security definition
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        config.GetConnectionString("IdentityDb") ?? string.Empty,
        name: "sqlserver",
        tags: ["db", "sql"])
    .AddRedis(
        config.GetConnectionString("Redis") ?? string.Empty,
        name: "redis",
        tags: ["cache", "redis"]);

var app = builder.Build();

// Middleware pipeline
app.UseExceptionHandlingMiddleware();

app.UseCors(options => options
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service V1");
    options.SwaggerEndpoint("/swagger/legacy/swagger.json", "Legacy SSO");
});

app.UseHealthChecks("/api/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponseNoExceptionDetails
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
