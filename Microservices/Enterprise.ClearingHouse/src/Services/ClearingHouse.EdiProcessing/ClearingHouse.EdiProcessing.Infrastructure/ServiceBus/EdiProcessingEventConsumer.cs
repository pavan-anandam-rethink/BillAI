using System.Text.Json;
using Azure.Messaging.ServiceBus;
using ClearingHouse.EdiProcessing.Application.Commands;
using ClearingHouse.EdiProcessing.Infrastructure.Configuration;
using ClearingHouse.SharedKernel;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClearingHouse.EdiProcessing.Infrastructure.ServiceBus;

/// <summary>
/// Background service that consumes file-ingested events from Azure Service Bus
/// and triggers EDI processing for each received file.
/// </summary>
public sealed class EdiProcessingEventConsumer : BackgroundService
{
    private readonly ILogger<EdiProcessingEventConsumer> _logger;
    private readonly IMediator _mediator;
    private readonly EdiProcessingOptions _options;
    private readonly ServiceBusClient _serviceBusClient;
    private ServiceBusProcessor? _processor;

    /// <summary>
    /// Initializes a new instance of the <see cref="EdiProcessingEventConsumer"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="mediator">The MediatR mediator.</param>
    /// <param name="options">The EDI processing configuration options.</param>
    /// <param name="serviceBusClient">The Azure Service Bus client.</param>
    public EdiProcessingEventConsumer(
        ILogger<EdiProcessingEventConsumer> logger,
        IMediator mediator,
        IOptions<EdiProcessingOptions> options,
        ServiceBusClient serviceBusClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "EDI Processing Event Consumer starting. Topic: {TopicName}, Subscription: {SubscriptionName}",
            _options.TopicName,
            _options.SubscriptionName);

        _processor = _serviceBusClient.CreateProcessor(
            _options.TopicName,
            _options.SubscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = _options.ChannelCapacity,
                PrefetchCount = 10
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken).ConfigureAwait(false);

        _logger.LogInformation("EDI Processing Event Consumer started successfully");

        // Keep the service running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("EDI Processing Event Consumer stopping gracefully");
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EDI Processing Event Consumer stopping");

        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);
            await _processor.DisposeAsync().ConfigureAwait(false);
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToString();

        _logger.LogDebug(
            "Received message {MessageId} from topic {TopicName}",
            args.Message.MessageId,
            _options.TopicName);

        try
        {
            var fileEvent = JsonSerializer.Deserialize<FileIngestedEvent>(
                messageBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (fileEvent is null)
            {
                _logger.LogWarning("Failed to deserialize message {MessageId}", args.Message.MessageId);
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "DeserializationFailed",
                    "Could not deserialize message body").ConfigureAwait(false);
                return;
            }

            var command = new ProcessEdiFileCommand(
                FileReference: fileEvent.FileReference,
                TransactionType: fileEvent.TransactionType,
                ClearinghouseType: fileEvent.ClearinghouseType,
                CorrelationId: fileEvent.CorrelationId);

            var result = await _mediator.Send(command, args.CancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await args.CompleteMessageAsync(args.Message).ConfigureAwait(false);
                _logger.LogInformation(
                    "Successfully processed EDI file. CorrelationId: {CorrelationId}",
                    fileEvent.CorrelationId);
            }
            else
            {
                var deliveryCount = args.Message.DeliveryCount;
                if (deliveryCount >= _options.MaxRetryCount)
                {
                    _logger.LogError(
                        "Max retries ({MaxRetries}) exceeded for message {MessageId}. Dead-lettering",
                        _options.MaxRetryCount,
                        args.Message.MessageId);

                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "MaxRetriesExceeded",
                        $"Failed after {deliveryCount} attempts: {result.ErrorMessage}").ConfigureAwait(false);
                }
                else
                {
                    _logger.LogWarning(
                        "Processing failed for message {MessageId}. Attempt {Attempt}/{MaxRetries}. Abandoning for retry",
                        args.Message.MessageId,
                        deliveryCount,
                        _options.MaxRetryCount);

                    await args.AbandonMessageAsync(args.Message).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing message {MessageId}", args.Message.MessageId);

            if (args.Message.DeliveryCount >= _options.MaxRetryCount)
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "UnhandledException",
                    ex.Message).ConfigureAwait(false);
            }
            else
            {
                await args.AbandonMessageAsync(args.Message).ConfigureAwait(false);
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus processing error. Source: {ErrorSource}, Entity: {EntityPath}",
            args.ErrorSource,
            args.FullyQualifiedNamespace);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Internal event model representing a file-ingested event from Service Bus.
    /// </summary>
    private sealed record FileIngestedEvent(
        FileReference FileReference,
        EdiTransactionType TransactionType,
        ClearinghouseType ClearinghouseType,
        CorrelationId CorrelationId);
}
