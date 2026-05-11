using EdiProcessing.Application;
using EdiProcessing.Infrastructure;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ClearingHouse.SharedKernel.Observability;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(TelemetryConstants.ActivitySources.EdiProcessing))
        .AddSource(TelemetryConstants.ActivitySources.EdiProcessing)
        .AddOtlpExporter());

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.AddHostedService<EdiProcessing.Worker.Workers.EdiProcessingWorker>();

var host = builder.Build();
host.Run();
