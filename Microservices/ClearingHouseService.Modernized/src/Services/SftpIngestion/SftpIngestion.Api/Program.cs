using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using ClearingHouse.SharedKernel.Messaging;
using ClearingHouse.SharedKernel.Observability;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SftpIngestion.Application.Behaviors;
using SftpIngestion.Application.Commands;
using SftpIngestion.Domain.Interfaces;
using SftpIngestion.Infrastructure.Messaging;
using SftpIngestion.Infrastructure.Persistence;
using SftpIngestion.Infrastructure.Sftp;
using SftpIngestion.Infrastructure.Workers;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("SftpIngestionService"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAzureMonitorTraceExporter(o => o.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PollClearinghouseCommand>());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// Database
builder.Services.AddDbContext<SftpIngestionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SftpIngestionDb")));

// Repositories
builder.Services.AddScoped<IInboundFileRepository, InboundFileRepository>();

// Azure Services
var keyVaultUri = new Uri(builder.Configuration["KeyVault:VaultUri"]!);
var credential = new DefaultAzureCredential();
builder.Services.AddSingleton(new SecretClient(keyVaultUri, credential));
builder.Services.AddSingleton(new ServiceBusClient(builder.Configuration.GetConnectionString("ServiceBus"), credential));

// SFTP
builder.Services.AddSingleton<ISftpConnectionPool>(sp =>
    new SftpConnectionPool(sp.GetRequiredService<SecretClient>(), sp.GetRequiredService<ILoggerFactory>()));

// Messaging
builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();
builder.Services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();

// Background Worker
builder.Services.AddHostedService<SftpPollingWorker>();

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

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
