using Azure.Messaging.ServiceBus;
using MediatR;
using System.Text.Json;

namespace EdiProcessing.Worker.Workers;

public class EdiProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EdiProcessingWorker> _logger;
    private readonly IConfiguration _configuration;

    public EdiProcessingWorker(IServiceProvider serviceProvider, ILogger<EdiProcessingWorker> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EDI Processing Worker started");

        var connectionString = _configuration.GetConnectionString("ServiceBus");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("Service Bus connection string not configured. Worker will idle.");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        await using var client = new ServiceBusClient(connectionString);
        var queueName = _configuration["ServiceBus:EdiProcessingQueue"] ?? "edi-processing-queue";
        await using var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,
            AutoCompleteMessages = false,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(30)
        });

        processor.ProcessMessageAsync += async args =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                _logger.LogInformation("Processing EDI message {MessageId}", args.Message.MessageId);
                await args.CompleteMessageAsync(args.Message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", args.Message.MessageId);
                if (args.Message.DeliveryCount >= 3)
                    await args.DeadLetterMessageAsync(args.Message, ex.Message, cancellationToken: stoppingToken);
                else
                    await args.AbandonMessageAsync(args.Message, cancellationToken: stoppingToken);
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Service Bus processor error: {ErrorSource}", args.ErrorSource);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
