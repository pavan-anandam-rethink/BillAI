using ClaimLifecycleMonitoring.Application.Abstractions.Notifications;
using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Application.Abstractions.Time;
using ClaimLifecycleMonitoring.Application.Common.Mapping;
using ClaimLifecycleMonitoring.Contracts.Claims;
using ClaimLifecycleMonitoring.Domain.Enums;
using ClaimLifecycleMonitoring.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClaimLifecycleMonitoring.Application.Features.Claims.Commands;

/// <summary>Command to record a failure on a claim.</summary>
public sealed record RecordFailureCommand(long ClaimId, RecordFailureRequest Request) : IRequest<ClaimDto>;

/// <summary>Handles <see cref="RecordFailureCommand"/>.</summary>
public sealed class RecordFailureCommandHandler : IRequestHandler<RecordFailureCommand, ClaimDto>
{
    private readonly IClaimRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IClaimNotificationPublisher _publisher;
    private readonly ILogger<RecordFailureCommandHandler> _logger;

    /// <summary>Creates a new <see cref="RecordFailureCommandHandler"/>.</summary>
    public RecordFailureCommandHandler(
        IClaimRepository repository,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IClaimNotificationPublisher publisher,
        ILogger<RecordFailureCommandHandler> logger)
    {
        _repository = repository;
        _uow = uow;
        _clock = clock;
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ClaimDto> Handle(RecordFailureCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(typeof(FailureCategory), command.Request.FailureCategory))
        {
            throw new DomainValidationException($"Failure category '{command.Request.FailureCategory}' is not valid.");
        }

        var claim = await _repository.GetByIdAsync(command.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Domain.Entities.Claim), command.ClaimId.ToString());

        claim.RecordFailure(
            (FailureCategory)command.Request.FailureCategory,
            command.Request.ExceptionMessage ?? string.Empty,
            command.Request.RetryAvailable,
            command.Request.ManualInterventionRequired,
            _clock.UtcNow);

        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogWarning("Recorded failure on claim {ClaimNumber}: {Category}", claim.ClaimNumber, (FailureCategory)command.Request.FailureCategory);

        var dto = claim.ToDto(_clock);
        await _publisher.PublishClaimUpdatedAsync(dto, cancellationToken).ConfigureAwait(false);
        return dto;
    }
}
