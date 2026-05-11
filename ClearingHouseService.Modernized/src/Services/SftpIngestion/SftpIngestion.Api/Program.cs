using SftpIngestion.Application;
using SftpIngestion.Infrastructure;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ClearingHouse.SharedKernel.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(TelemetryConstants.ActivitySources.SftpIngestion))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(TelemetryConstants.ActivitySources.SftpIngestion)
        .AddOtlpExporter());

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "SFTP Ingestion Service", Version = "v1" }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
