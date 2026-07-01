using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Application.Abstractions.Time;
using ClaimLifecycleMonitoring.Application.Common.Mapping;
using ClaimLifecycleMonitoring.Contracts.Claims;
using ClaimLifecycleMonitoring.Contracts.Common;
using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;
using ClaimLifecycleMonitoring.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimLifecycleMonitoring.Application.Features.Claims.Queries;

/// <summary>Query returning a paged claim list matching search filters.</summary>
public sealed record SearchClaimsQuery(ClaimSearchQuery Filter) : IRequest<PagedResult<ClaimDto>>;

/// <summary>Query returning a single claim by identifier.</summary>
public sealed record GetClaimByIdQuery(long ClaimId) : IRequest<ClaimDto>;

/// <summary>Query returning a single claim by claim number.</summary>
public sealed record GetClaimByNumberQuery(string ClaimNumber) : IRequest<ClaimDto>;

/// <summary>Query returning the ordered history for a claim.</summary>
public sealed record GetClaimHistoryQuery(long ClaimId) : IRequest<IReadOnlyList<ClaimHistoryEntryDto>>;

/// <summary>Handles <see cref="SearchClaimsQuery"/>.</summary>
public sealed class SearchClaimsQueryHandler : IRequestHandler<SearchClaimsQuery, PagedResult<ClaimDto>>
{
    private readonly IClaimRepository _repository;
    private readonly IDateTimeProvider _clock;

    /// <summary>Creates a new <see cref="SearchClaimsQueryHandler"/>.</summary>
    public SearchClaimsQueryHandler(IClaimRepository repository, IDateTimeProvider clock)
    {
        _repository = repository;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<PagedResult<ClaimDto>> Handle(SearchClaimsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var f = request.Filter ?? new ClaimSearchQuery();
        var page = Math.Max(0, f.Page);
        var pageSize = Math.Clamp(f.PageSize <= 0 ? 50 : f.PageSize, 1, 500);

        IQueryable<Claim> q = _repository.Query();

        if (!string.IsNullOrWhiteSpace(f.SearchTerm))
        {
            var term = f.SearchTerm.Trim();
            q = q.Where(c =>
                EF.Functions.Like(c.ClaimNumber, $"%{term}%") ||
                EF.Functions.Like(c.PatientName, $"%{term}%") ||
                EF.Functions.Like(c.ProviderName, $"%{term}%") ||
                EF.Functions.Like(c.CustomerName, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(f.CustomerCode))
        {
            q = q.Where(c => c.CustomerCode == f.CustomerCode);
        }

        if (!string.IsNullOrWhiteSpace(f.AccountCode))
        {
            q = q.Where(c => c.AccountCode == f.AccountCode);
        }

        if (f.CurrentStage.HasValue && Enum.IsDefined(typeof(ClaimStage), f.CurrentStage.Value))
        {
            var stage = (ClaimStage)f.CurrentStage.Value;
            q = q.Where(c => c.CurrentStage == stage);
        }

        if (f.CurrentStatus.HasValue && Enum.IsDefined(typeof(ClaimStatus), f.CurrentStatus.Value))
        {
            var status = (ClaimStatus)f.CurrentStatus.Value;
            q = q.Where(c => c.CurrentStatus == status);
        }

        if (f.FailureCategory.HasValue && Enum.IsDefined(typeof(FailureCategory), f.FailureCategory.Value))
        {
            var cat = (FailureCategory)f.FailureCategory.Value;
            q = q.Where(c => c.FailureCategory == cat);
        }

        if (f.ManualInterventionRequired == true)
        {
            q = q.Where(c => c.ManualInterventionRequired);
        }

        if (f.Stuck == true)
        {
            q = q.Where(c => c.CurrentStatus == ClaimStatus.Stuck);
        }

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q
            .OrderByDescending(c => c.ModifiedUtc ?? c.CreatedUtc)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = items.Select(c => c.ToDto(_clock)).ToList();
        return new PagedResult<ClaimDto>(page, pageSize, total, dtos);
    }
}

/// <summary>Handles <see cref="GetClaimByIdQuery"/>.</summary>
public sealed class GetClaimByIdQueryHandler : IRequestHandler<GetClaimByIdQuery, ClaimDto>
{
    private readonly IClaimRepository _repository;
    private readonly IDateTimeProvider _clock;

    /// <summary>Creates a new instance.</summary>
    public GetClaimByIdQueryHandler(IClaimRepository repository, IDateTimeProvider clock)
    {
        _repository = repository;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<ClaimDto> Handle(GetClaimByIdQuery request, CancellationToken cancellationToken)
    {
        var claim = await _repository.GetByIdAsync(request.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Claim), request.ClaimId.ToString());
        return claim.ToDto(_clock);
    }
}

/// <summary>Handles <see cref="GetClaimByNumberQuery"/>.</summary>
public sealed class GetClaimByNumberQueryHandler : IRequestHandler<GetClaimByNumberQuery, ClaimDto>
{
    private readonly IClaimRepository _repository;
    private readonly IDateTimeProvider _clock;

    /// <summary>Creates a new instance.</summary>
    public GetClaimByNumberQueryHandler(IClaimRepository repository, IDateTimeProvider clock)
    {
        _repository = repository;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<ClaimDto> Handle(GetClaimByNumberQuery request, CancellationToken cancellationToken)
    {
        var claim = await _repository.GetByClaimNumberAsync(request.ClaimNumber, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Claim), request.ClaimNumber);
        return claim.ToDto(_clock);
    }
}

/// <summary>Handles <see cref="GetClaimHistoryQuery"/>.</summary>
public sealed class GetClaimHistoryQueryHandler : IRequestHandler<GetClaimHistoryQuery, IReadOnlyList<ClaimHistoryEntryDto>>
{
    private readonly IClaimRepository _repository;

    /// <summary>Creates a new instance.</summary>
    public GetClaimHistoryQueryHandler(IClaimRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClaimHistoryEntryDto>> Handle(GetClaimHistoryQuery request, CancellationToken cancellationToken)
    {
        var claim = await _repository.GetByIdAsync(request.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Claim), request.ClaimId.ToString());
        return claim.History
            .OrderBy(h => h.OccurredUtc)
            .Select(h => h.ToDto())
            .ToList();
    }
}
