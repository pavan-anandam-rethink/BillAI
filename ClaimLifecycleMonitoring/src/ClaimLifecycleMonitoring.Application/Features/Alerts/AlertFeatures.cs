using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Application.Abstractions.Time;
using ClaimLifecycleMonitoring.Application.Common.Mapping;
using ClaimLifecycleMonitoring.Contracts.Alerts;
using ClaimLifecycleMonitoring.Contracts.Claims;
using ClaimLifecycleMonitoring.Contracts.Common;
using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimLifecycleMonitoring.Application.Features.Alerts;

/// <summary>Query returning a paged alert list.</summary>
public sealed record SearchAlertsQuery(bool? Acknowledged, int Page, int PageSize) : IRequest<PagedResult<ClaimAlertDto>>;

/// <summary>Command acknowledging an alert.</summary>
public sealed record AcknowledgeAlertCommand(long AlertId, AcknowledgeAlertRequest Request) : IRequest<ClaimAlertDto>;

/// <summary>Handles <see cref="SearchAlertsQuery"/>.</summary>
public sealed class SearchAlertsQueryHandler : IRequestHandler<SearchAlertsQuery, PagedResult<ClaimAlertDto>>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IClaimRepository _claimRepository;

    /// <summary>Creates a new instance.</summary>
    public SearchAlertsQueryHandler(IAlertRepository alertRepository, IClaimRepository claimRepository)
    {
        _alertRepository = alertRepository;
        _claimRepository = claimRepository;
    }

    /// <inheritdoc />
    public async Task<PagedResult<ClaimAlertDto>> Handle(SearchAlertsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(0, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 50 : request.PageSize, 1, 500);
        var q = _alertRepository.Query();
        if (request.Acknowledged.HasValue)
        {
            q = q.Where(a => a.Acknowledged == request.Acknowledged.Value);
        }

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var alerts = await q
            .OrderByDescending(a => a.RaisedUtc)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var claimIds = alerts.Select(a => a.ClaimId).Distinct().ToList();
        var claims = await _claimRepository.Query()
            .Where(c => claimIds.Contains(c.Id))
            .Select(c => new { c.Id, c.ClaimNumber })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var lookup = claims.ToDictionary(c => c.Id, c => c.ClaimNumber);

        var items = alerts.Select(a =>
        {
            var claimNumber = lookup.TryGetValue(a.ClaimId, out var num) ? num : string.Empty;
            return a.ToDto(claimNumber);
        }).ToList();

        return new PagedResult<ClaimAlertDto>(page, pageSize, total, items);
    }
}

/// <summary>Handles <see cref="AcknowledgeAlertCommand"/>.</summary>
public sealed class AcknowledgeAlertCommandHandler : IRequestHandler<AcknowledgeAlertCommand, ClaimAlertDto>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IClaimRepository _claimRepository;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    /// <summary>Creates a new instance.</summary>
    public AcknowledgeAlertCommandHandler(IAlertRepository alertRepository, IClaimRepository claimRepository, IUnitOfWork uow, IDateTimeProvider clock)
    {
        _alertRepository = alertRepository;
        _claimRepository = claimRepository;
        _uow = uow;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<ClaimAlertDto> Handle(AcknowledgeAlertCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var alert = await _alertRepository.GetByIdAsync(command.AlertId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(ClaimAlert), command.AlertId.ToString());
        alert.Acknowledge(command.Request.UserId ?? "system", _clock.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var claim = await _claimRepository.Query()
            .Where(c => c.Id == alert.ClaimId)
            .Select(c => new { c.ClaimNumber })
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return alert.ToDto(claim?.ClaimNumber ?? string.Empty);
    }
}
