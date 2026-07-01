using ClaimLifecycleMonitoring.Application.DependencyInjection;
using ClaimLifecycleMonitoring.Infrastructure.DependencyInjection;
using ClaimLifecycleMonitoring.Infrastructure.Notifications;
using ClaimLifecycleMonitoring.Persistence;
using ClaimLifecycleMonitoring.Persistence.DependencyInjection;
using ClaimLifecycleMonitoring.Persistence.Seeding;
using ClaimLifecycleMonitoring.Worker;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "ClaimLifecycleMonitoring.Worker")
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

builder.Services.AddClaimLifecycleApplication(builder.Configuration);
builder.Services.AddClaimLifecyclePersistence(builder.Configuration);
builder.Services.AddClaimLifecycleInfrastructure();
builder.Services.AddSingleton<IRealtimeClaimBroadcaster, NullRealtimeClaimBroadcaster>();

var appInsightsConn = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsConn))
{
    builder.Services.AddApplicationInsightsTelemetryWorkerService(o => o.ConnectionString = appInsightsConn);
}

builder.Services.AddHostedService<ClaimMonitoringWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
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
        logger.LogError(ex, "Failed to initialize database from worker.");
    }
}

host.Run();
