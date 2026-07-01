using ClaimLifecycleMonitoring.Application.Abstractions.Notifications;
using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Application.Abstractions.Time;
using ClaimLifecycleMonitoring.Application.Common.Mapping;
using ClaimLifecycleMonitoring.Contracts.Claims;
using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ClaimLifecycleMonitoring.Application.Configuration;

namespace ClaimLifecycleMonitoring.Application.Features.Claims.Commands;

/// <summary>
/// Command to register a new claim under monitoring.
/// </summary>
/// <param name="Request">The create-claim request payload.</param>
public sealed record CreateClaimCommand(CreateClaimRequest Request) : IRequest<ClaimDto>;

/// <summary>Handles <see cref="CreateClaimCommand"/>.</summary>
public sealed class CreateClaimCommandHandler : IRequestHandler<CreateClaimCommand, ClaimDto>
{
    private readonly IClaimRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IClaimNotificationPublisher _publisher;
    private readonly ILogger<CreateClaimCommandHandler> _logger;
    private readonly MonitoringOptions _options;

    /// <summary>Creates a new <see cref="CreateClaimCommandHandler"/>.</summary>
    public CreateClaimCommandHandler(
        IClaimRepository repository,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IClaimNotificationPublisher publisher,
        IOptions<MonitoringOptions> options,
        ILogger<CreateClaimCommandHandler> logger)
    {
        _repository = repository;
        _uow = uow;
        _clock = clock;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ClaimDto> Handle(CreateClaimCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var request = command.Request;

        if (await _repository.ExistsAsync(request.ClaimNumber, cancellationToken).ConfigureAwait(false))
        {
            throw new DomainValidationException($"Claim with number '{request.ClaimNumber}' already exists.");
        }

        var claim = Claim.Create(
            request.ClaimNumber,
            request.CustomerCode,
            request.CustomerName,
            request.AccountCode,
            request.AccountName,
            request.PatientId,
            request.PatientName,
            request.ProviderId,
            request.ProviderName,
            request.BilledAmount,
            _options.DefaultStageSlaHours,
            _clock.UtcNow);

        await _repository.AddAsync(claim, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Registered claim {ClaimNumber} for customer {Customer}",
            claim.ClaimNumber, claim.CustomerCode);

        var dto = claim.ToDto(_clock);
        await _publisher.PublishClaimUpdatedAsync(dto, cancellationToken).ConfigureAwait(false);
        return dto;
    }
}
