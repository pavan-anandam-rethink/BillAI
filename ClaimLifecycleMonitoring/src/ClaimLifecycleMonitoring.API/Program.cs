using ClaimLifecycleMonitoring.API.Middleware;
using ClaimLifecycleMonitoring.API.Realtime;
using ClaimLifecycleMonitoring.Application.DependencyInjection;
using ClaimLifecycleMonitoring.Infrastructure.DependencyInjection;
using ClaimLifecycleMonitoring.Infrastructure.Notifications;
using ClaimLifecycleMonitoring.Persistence;
using ClaimLifecycleMonitoring.Persistence.DependencyInjection;
using ClaimLifecycleMonitoring.Persistence.Seeding;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// Serilog structured logging with console, rolling file and Application Insights
// -----------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, logger) =>
{
    logger
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "ClaimLifecycleMonitoring.API")
        .WriteTo.Console()
        .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
});

// -----------------------------------------------------------------------------
// Application, Persistence and Infrastructure layers
// -----------------------------------------------------------------------------
builder.Services.AddClaimLifecycleApplication(builder.Configuration);
builder.Services.AddClaimLifecyclePersistence(builder.Configuration);
builder.Services.AddClaimLifecycleInfrastructure();
builder.Services.AddSingleton<IRealtimeClaimBroadcaster, SignalRClaimBroadcaster>();

// -----------------------------------------------------------------------------
// ASP.NET Core / SignalR / Swagger
// -----------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = false);

builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Claim Lifecycle Monitoring API",
        Version = "v1",
        Description = "Centralized claim lifecycle monitoring, tracking, alerting and maintenance for the healthcare billing platform."
    });

    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// -----------------------------------------------------------------------------
// Application Insights (optional – enabled when a connection string is set)
// -----------------------------------------------------------------------------
var appInsightsConn = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsConn))
{
    builder.Services.AddApplicationInsightsTelemetry(o => o.ConnectionString = appInsightsConn);
}

// -----------------------------------------------------------------------------
// Health checks
// -----------------------------------------------------------------------------
var healthChecks = builder.Services.AddHealthChecks()
    .AddDbContextCheck<ClaimLifecycleDbContext>("database");

var sqlConnection = builder.Configuration.GetConnectionString(PersistenceServiceCollectionExtensions.ConnectionStringName);
if (!string.IsNullOrWhiteSpace(sqlConnection) && sqlConnection.Contains("Server=", StringComparison.OrdinalIgnoreCase))
{
    healthChecks.AddSqlServer(sqlConnection, name: "sqlserver", tags: new[] { "ready" });
}

// -----------------------------------------------------------------------------
// CORS
// -----------------------------------------------------------------------------
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowed(_ => true)
    .AllowCredentials()));

var app = builder.Build();

// -----------------------------------------------------------------------------
// Middleware pipeline
// -----------------------------------------------------------------------------
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "Claim Lifecycle Monitoring API v1");
        o.RoutePrefix = "swagger";
    });
}

app.UseCors();

app.MapControllers();
app.MapHub<ClaimMonitoringHub>(ClaimMonitoringHub.Path);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// -----------------------------------------------------------------------------
// Ensure database schema exists and seed baseline data
// -----------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClaimLifecycleDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }
        else
        {
            db.Database.EnsureCreated();
        }
        await ClaimLifecycleSeeder.SeedAsync(db, logger, CancellationToken.None).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize database.");
    }
}

app.Run();

/// <summary>Program entry point marker (exposed for integration tests).</summary>
public partial class Program { }
