using BillingService.App.Infrastructure;
using BillingService.App.Persistence;
using BillingService.App.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddBillingPersistence(builder.Configuration);
builder.Services.AddBillingInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxPublisherWorker>();

var host = builder.Build();
host.Run();

