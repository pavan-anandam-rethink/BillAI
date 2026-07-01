using ClaimLifecycleMonitoring.Application.Abstractions.Notifications;
using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Application.Abstractions.Time;
using ClaimLifecycleMonitoring.Application.Common.Mapping;
using ClaimLifecycleMonitoring.Contracts.Claims;
using ClaimLifecycleMonitoring.Domain.Common;
using ClaimLifecycleMonitoring.Domain.Enums;
using ClaimLifecycleMonitoring.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClaimLifecycleMonitoring.Application.Features.Claims.Commands;

/// <summary>Command to advance a claim to the next lifecycle stage.</summary>
public sealed record AdvanceClaimStageCommand(long ClaimId, AdvanceClaimStageRequest Request) : IRequest<ClaimDto>;

/// <summary>Handles <see cref="AdvanceClaimStageCommand"/>.</summary>
public sealed class AdvanceClaimStageCommandHandler : IRequestHandler<AdvanceClaimStageCommand, ClaimDto>
{
    private readonly IClaimRepository _repository;
    private readonly IStageSlaConfigurationRepository _slaRepository;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IClaimNotificationPublisher _publisher;
    private readonly ILogger<AdvanceClaimStageCommandHandler> _logger;

    /// <summary>Creates a new <see cref="AdvanceClaimStageCommandHandler"/>.</summary>
    public AdvanceClaimStageCommandHandler(
        IClaimRepository repository,
        IStageSlaConfigurationRepository slaRepository,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IClaimNotificationPublisher publisher,
        ILogger<AdvanceClaimStageCommandHandler> logger)
    {
        _repository = repository;
        _slaRepository = slaRepository;
        _uow = uow;
        _clock = clock;
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ClaimDto> Handle(AdvanceClaimStageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!Enum.IsDefined(typeof(ClaimStage), command.Request.TargetStage))
        {
            throw new DomainValidationException($"Target stage '{command.Request.TargetStage}' is not a valid claim stage.");
        }

        var targetStage = (ClaimStage)command.Request.TargetStage;

        var claim = await _repository.GetByIdAsync(command.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Domain.Entities.Claim), command.ClaimId.ToString());

        var next = ClaimLifecyclePipeline.NextStage(targetStage);
        var slaForNext = next.HasValue
            ? (await _slaRepository.GetForStageAsync(next.Value, cancellationToken).ConfigureAwait(false)).SlaHours
            : 0;

        claim.AdvanceToStage(targetStage, next, slaForNext, command.Request.Message, _clock.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Advanced claim {ClaimNumber} to stage {Stage}", claim.ClaimNumber, targetStage);

        var dto = claim.ToDto(_clock);
        await _publisher.PublishClaimUpdatedAsync(dto, cancellationToken).ConfigureAwait(false);
        return dto;
    }
}
