using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using ClearingHouse.SharedKernel.Domain.Interfaces;
using ClearingHouse.SftpIngestion.Api.HealthChecks;
using ClearingHouse.SftpIngestion.Api.Middleware;
using ClearingHouse.SftpIngestion.Application.Handlers;
using ClearingHouse.SftpIngestion.Application.Validators;
using ClearingHouse.SftpIngestion.Domain.Entities;
using ClearingHouse.SftpIngestion.Domain.Interfaces;
using ClearingHouse.SftpIngestion.Infrastructure.Blob;
using ClearingHouse.SftpIngestion.Infrastructure.Configuration;
using ClearingHouse.SftpIngestion.Infrastructure.Persistence;
using ClearingHouse.SftpIngestion.Infrastructure.ServiceBus;
using ClearingHouse.SftpIngestion.Infrastructure.Sftp;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<SftpIngestionOptions>(
    builder.Configuration.GetSection(SftpIngestionOptions.SectionName));

var options = builder.Configuration
    .GetSection(SftpIngestionOptions.SectionName)
    .Get<SftpIngestionOptions>() ?? new SftpIngestionOptions();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<StartIngestionCommandHandler>());

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<StartIngestionCommandValidator>();

// Entity Framework Core
builder.Services.AddDbContext<IngestionDbContext>(dbOptions =>
    dbOptions.UseSqlServer(options.DatabaseConnectionString, sql =>
        sql.EnableRetryOnFailure(maxRetryCount: 3)));

// Azure Blob Storage
builder.Services.AddSingleton(_ => new BlobServiceClient(options.BlobStorageConnectionString));
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// Azure Service Bus
builder.Services.AddSingleton(sp =>
{
    var client = new ServiceBusClient(options.ServiceBusConnectionString);
    return client.CreateSender(options.ServiceBusTopicName);
});
builder.Services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();

// SFTP
builder.Services.AddTransient<ISftpClient, SshNetSftpClient>();
builder.Services.AddSingleton<ISftpConnectionPool, SftpConnectionPool>();

// Repository
builder.Services.AddScoped<IRepository<SftpIngestionJob>, IngestionJobRepository>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("ClearingHouse.SftpIngestion"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("ClearingHouse.SftpIngestion")
        .AddOtlpExporter());

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(options.DatabaseConnectionString, name: "database", tags: ["ready"])
    .AddCheck<SftpHealthCheck>("sftp", tags: ["ready"]);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SFTP Ingestion Service",
        Version = "v1",
        Description = "Service for polling clearinghouse SFTP endpoints and ingesting EDI files."
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// Middleware
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SFTP Ingestion Service v1"));
}

app.MapControllers();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Liveness: always healthy if app is running
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
