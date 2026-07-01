using ClaimLifecycleMonitoring.Application.Abstractions.Notifications;
using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Application.Abstractions.Time;
using ClaimLifecycleMonitoring.Application.Common.Mapping;
using ClaimLifecycleMonitoring.Contracts.Claims;
using ClaimLifecycleMonitoring.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClaimLifecycleMonitoring.Application.Features.Claims.Commands;

/// <summary>Command to retry a failed claim.</summary>
public sealed record RetryClaimCommand(long ClaimId, RetryClaimRequest Request) : IRequest<ClaimDto>;

/// <summary>Command to cancel a claim.</summary>
public sealed record CancelClaimCommand(long ClaimId, CancelClaimRequest Request) : IRequest<ClaimDto>;

/// <summary>Command to apply a payment to a claim.</summary>
public sealed record ApplyPaymentCommand(long ClaimId, ApplyPaymentRequest Request) : IRequest<ClaimDto>;

/// <summary>Command to mark a claim as a duplicate of another.</summary>
public sealed record MarkDuplicateCommand(long ClaimId, string DuplicateOfClaimNumber) : IRequest<ClaimDto>;

/// <summary>Handles <see cref="RetryClaimCommand"/>.</summary>
public sealed class RetryClaimCommandHandler : IRequestHandler<RetryClaimCommand, ClaimDto>
{
    private readonly IClaimRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IClaimNotificationPublisher _publisher;
    private readonly ILogger<RetryClaimCommandHandler> _logger;

    /// <summary>Creates a new instance.</summary>
    public RetryClaimCommandHandler(IClaimRepository repository, IUnitOfWork uow, IDateTimeProvider clock,
        IClaimNotificationPublisher publisher, ILogger<RetryClaimCommandHandler> logger)
    { _repository = repository; _uow = uow; _clock = clock; _publisher = publisher; _logger = logger; }

    /// <inheritdoc />
    public async Task<ClaimDto> Handle(RetryClaimCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await _repository.GetByIdAsync(command.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Domain.Entities.Claim), command.ClaimId.ToString());
        claim.RecordRetry(command.Request.Message ?? "Manual retry requested.", _clock.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retried claim {ClaimNumber} (attempt {RetryCount})", claim.ClaimNumber, claim.RetryCount);
        var dto = claim.ToDto(_clock);
        await _publisher.PublishClaimUpdatedAsync(dto, cancellationToken).ConfigureAwait(false);
        return dto;
    }
}

/// <summary>Handles <see cref="CancelClaimCommand"/>.</summary>
public sealed class CancelClaimCommandHandler : IRequestHandler<CancelClaimCommand, ClaimDto>
{
    private readonly IClaimRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IClaimNotificationPublisher _publisher;
    private readonly ILogger<CancelClaimCommandHandler> _logger;

    /// <summary>Creates a new instance.</summary>
    public CancelClaimCommandHandler(IClaimRepository repository, IUnitOfWork uow, IDateTimeProvider clock,
        IClaimNotificationPublisher publisher, ILogger<CancelClaimCommandHandler> logger)
    { _repository = repository; _uow = uow; _clock = clock; _publisher = publisher; _logger = logger; }

    /// <inheritdoc />
    public async Task<ClaimDto> Handle(CancelClaimCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await _repository.GetByIdAsync(command.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Domain.Entities.Claim), command.ClaimId.ToString());
        claim.Cancel(command.Request.Reason ?? "Cancelled.", _clock.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Cancelled claim {ClaimNumber}", claim.ClaimNumber);
        var dto = claim.ToDto(_clock);
        await _publisher.PublishClaimUpdatedAsync(dto, cancellationToken).ConfigureAwait(false);
        return dto;
    }
}

/// <summary>Handles <see cref="ApplyPaymentCommand"/>.</summary>
public sealed class ApplyPaymentCommandHandler : IRequestHandler<ApplyPaymentCommand, ClaimDto>
{
    private readonly IClaimRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IClaimNotificationPublisher _publisher;
    private readonly ILogger<ApplyPaymentCommandHandler> _logger;

    /// <summary>Creates a new instance.</summary>
    public ApplyPaymentCommandHandler(IClaimRepository repository, IUnitOfWork uow, IDateTimeProvider clock,
        IClaimNotificationPublisher publisher, ILogger<ApplyPaymentCommandHandler> logger)
    { _repository = repository; _uow = uow; _clock = clock; _publisher = publisher; _logger = logger; }

    /// <inheritdoc />
    public async Task<ClaimDto> Handle(ApplyPaymentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await _repository.GetByIdAsync(command.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Domain.Entities.Claim), command.ClaimId.ToString());
        claim.ApplyPayment(command.Request.Amount, _clock.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Applied payment {Amount} on claim {ClaimNumber}", command.Request.Amount, claim.ClaimNumber);
        var dto = claim.ToDto(_clock);
        await _publisher.PublishClaimUpdatedAsync(dto, cancellationToken).ConfigureAwait(false);
        return dto;
    }
}

/// <summary>Handles <see cref="MarkDuplicateCommand"/>.</summary>
public sealed class MarkDuplicateCommandHandler : IRequestHandler<MarkDuplicateCommand, ClaimDto>
{
    private readonly IClaimRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IClaimNotificationPublisher _publisher;
    private readonly ILogger<MarkDuplicateCommandHandler> _logger;

    /// <summary>Creates a new instance.</summary>
    public MarkDuplicateCommandHandler(IClaimRepository repository, IUnitOfWork uow, IDateTimeProvider clock,
        IClaimNotificationPublisher publisher, ILogger<MarkDuplicateCommandHandler> logger)
    { _repository = repository; _uow = uow; _clock = clock; _publisher = publisher; _logger = logger; }

    /// <inheritdoc />
    public async Task<ClaimDto> Handle(MarkDuplicateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await _repository.GetByIdAsync(command.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Domain.Entities.Claim), command.ClaimId.ToString());
        claim.MarkDuplicate(command.DuplicateOfClaimNumber ?? string.Empty, _clock.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogWarning("Marked claim {ClaimNumber} as duplicate of {OtherClaim}", claim.ClaimNumber, command.DuplicateOfClaimNumber);
        var dto = claim.ToDto(_clock);
        await _publisher.PublishClaimUpdatedAsync(dto, cancellationToken).ConfigureAwait(false);
        return dto;
    }
}
