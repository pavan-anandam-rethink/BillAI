using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using ClearingHouse.EdiProcessing.Application.Handlers;
using ClearingHouse.EdiProcessing.Application.Pipelines;
using ClearingHouse.EdiProcessing.Application.Validators;
using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.Interfaces;
using ClearingHouse.EdiProcessing.Infrastructure.Configuration;
using ClearingHouse.EdiProcessing.Infrastructure.Parsers;
using ClearingHouse.EdiProcessing.Infrastructure.Persistence;
using ClearingHouse.EdiProcessing.Infrastructure.ServiceBus;
using ClearingHouse.SharedKernel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<EdiProcessingOptions>(
    builder.Configuration.GetSection(EdiProcessingOptions.SectionName));

var ediOptions = builder.Configuration
    .GetSection(EdiProcessingOptions.SectionName)
    .Get<EdiProcessingOptions>() ?? new EdiProcessingOptions();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<ProcessEdiFileCommandHandler>();
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<ProcessEdiFileCommandValidator>();

// Entity Framework Core
builder.Services.AddDbContext<EdiProcessingDbContext>(options =>
{
    options.UseSqlServer(ediOptions.DatabaseConnectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
        sqlOptions.CommandTimeout(60);
    });
});

// Azure Blob Storage
builder.Services.AddSingleton(_ =>
    new BlobServiceClient(ediOptions.BlobStorageConnectionString));

// Azure Service Bus
builder.Services.AddSingleton(_ =>
    new ServiceBusClient(ediOptions.ServiceBusConnectionString));

// Domain & Application Services
builder.Services.AddScoped<IRepository<EdiFile>, EdiFileRepository>();
builder.Services.AddScoped<EdiFileRepository>();
builder.Services.AddSingleton<EdiParserFactory>();
builder.Services.AddScoped<IEdiProcessingPipeline, EdiProcessingPipeline>();

// Parser registrations
builder.Services.AddTransient<Edi837Parser>();
builder.Services.AddTransient<Edi835Parser>();
builder.Services.AddTransient<Edi999Parser>();
builder.Services.AddTransient<Edi277Parser>();

// Background Services
builder.Services.AddHostedService<EdiProcessingEventConsumer>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("ClearingHouse.EdiProcessing"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("ClearingHouse.EdiProcessing")
            .AddOtlpExporter();
    });

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        ediOptions.DatabaseConnectionString,
        name: "database",
        tags: ["ready"])
    .AddAzureServiceBusTopic(
        ediOptions.ServiceBusConnectionString,
        ediOptions.TopicName,
        name: "servicebus",
        tags: ["ready"]);

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "EDI Processing API",
        Version = "v1",
        Description = "API for EDI file processing, monitoring, and retry management"
    });
});

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Liveness: always healthy if app is running
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
