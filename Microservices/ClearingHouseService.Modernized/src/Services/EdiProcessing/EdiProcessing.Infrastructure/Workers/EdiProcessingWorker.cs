using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EdiProcessing.Application.Commands;
using EdiProcessing.Domain.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EdiProcessing.Infrastructure.Workers;

/// <summary>
/// Service Bus consumer that processes EDI files from the queue.
/// Supports concurrent processing with configurable parallelism.
/// Implements dead-letter queue handling for failed messages.
/// </summary>
public class EdiProcessingWorker : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EdiProcessingWorker> _logger;
    private readonly int _maxConcurrency;
    private ServiceBusProcessor? _processor;

    public EdiProcessingWorker(
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory,
        ILogger<EdiProcessingWorker> logger,
        int maxConcurrency = 10)
    {
        _serviceBusClient = serviceBusClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _maxConcurrency = maxConcurrency;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _serviceBusClient.CreateProcessor("edi-processing-queue", new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = _maxConcurrency,
            AutoCompleteMessages = false,
            PrefetchCount = _maxConcurrency * 2,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(30)
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        _logger.LogInformation("EDI Processing Worker starting with max concurrency: {MaxConcurrency}", _maxConcurrency);
        await _processor.StartProcessingAsync(stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;
        _logger.LogInformation("Processing EDI message {MessageId}, correlation: {CorrelationId}",
            message.MessageId, message.CorrelationId);

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            var command = JsonSerializer.Deserialize<ProcessEdiFileCommand>(message.Body.ToString())
                ?? throw new InvalidOperationException("Failed to deserialize EDI processing command");

            var result = await mediator.Send(command, args.CancellationToken);

            if (result.IsSuccess)
            {
                await args.CompleteMessageAsync(message, args.CancellationToken);
                _logger.LogInformation("EDI message {MessageId} processed successfully", message.MessageId);
            }
            else
            {
                if (message.DeliveryCount >= 5)
                {
                    await args.DeadLetterMessageAsync(message, "MaxRetriesExceeded",
                        $"Failed after {message.DeliveryCount} attempts: {result.Error}",
                        args.CancellationToken);
                    _logger.LogWarning("EDI message {MessageId} dead-lettered after {DeliveryCount} attempts",
                        message.MessageId, message.DeliveryCount);
                }
                else
                {
                    await args.AbandonMessageAsync(message, cancellationToken: args.CancellationToken);
                    _logger.LogWarning("EDI message {MessageId} abandoned for retry. Attempt {DeliveryCount}",
                        message.MessageId, message.DeliveryCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing EDI message {MessageId}", message.MessageId);

            if (message.DeliveryCount >= 5)
            {
                await args.DeadLetterMessageAsync(message, "ProcessingException", ex.Message, args.CancellationToken);
            }
            else
            {
                await args.AbandonMessageAsync(message, cancellationToken: args.CancellationToken);
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processing error. Source: {ErrorSource}, Entity: {EntityPath}",
            args.ErrorSource, args.FullyQualifiedNamespace);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
