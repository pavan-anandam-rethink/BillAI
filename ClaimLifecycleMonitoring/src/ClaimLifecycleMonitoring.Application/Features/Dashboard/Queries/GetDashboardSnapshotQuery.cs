using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Contracts.Dashboard;
using ClaimLifecycleMonitoring.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ClaimLifecycleMonitoring.Application.Abstractions.Time;

namespace ClaimLifecycleMonitoring.Application.Features.Dashboard.Queries;

/// <summary>
/// Query that returns a full <see cref="DashboardSnapshotDto"/> for the operations dashboard.
/// </summary>
public sealed record GetDashboardSnapshotQuery : IRequest<DashboardSnapshotDto>;

/// <summary>Handles <see cref="GetDashboardSnapshotQuery"/>.</summary>
public sealed class GetDashboardSnapshotQueryHandler : IRequestHandler<GetDashboardSnapshotQuery, DashboardSnapshotDto>
{
    private readonly IClaimRepository _repository;
    private readonly IDateTimeProvider _clock;

    /// <summary>Creates a new <see cref="GetDashboardSnapshotQueryHandler"/>.</summary>
    public GetDashboardSnapshotQueryHandler(IClaimRepository repository, IDateTimeProvider clock)
    {
        _repository = repository;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<DashboardSnapshotDto> Handle(GetDashboardSnapshotQuery request, CancellationToken cancellationToken)
    {
        var q = _repository.Query();

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var byStatus = await q
            .GroupBy(c => c.CurrentStatus)
            .Select(g => new { Status = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        long ByStatus(ClaimStatus s) => byStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0L;

        var byStageCounts = await q
            .GroupBy(c => c.CurrentStage)
            .Select(g => new { Stage = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        long Pending(ClaimStage stage) => byStageCounts.FirstOrDefault(x => x.Stage == stage)?.Count ?? 0L;

        var byFailureCategory = await q
            .Where(c => c.FailureCategory != FailureCategory.None)
            .GroupBy(c => c.FailureCategory)
            .Select(g => new { Category = g.Key, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var byCustomer = await q
            .GroupBy(c => new { c.CustomerCode, c.CustomerName })
            .Select(g => new { g.Key.CustomerCode, g.Key.CustomerName, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .Take(25)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var byAccount = await q
            .GroupBy(c => new { c.AccountCode, c.AccountName })
            .Select(g => new { g.Key.AccountCode, g.Key.AccountName, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .Take(25)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new DashboardSnapshotDto
        {
            GeneratedUtc = _clock.UtcNow,
            TotalClaims = total,
            InProgressClaims = ByStatus(ClaimStatus.InProgress),
            CompletedClaims = ByStatus(ClaimStatus.Completed),
            FailedClaims = ByStatus(ClaimStatus.Failed),
            WaitingClaims = ByStatus(ClaimStatus.Waiting),
            RejectedClaims = ByStatus(ClaimStatus.Rejected),
            PendingPaymentClaims = ByStatus(ClaimStatus.PendingPayment),
            Pending999Claims = Pending(ClaimStage.EdiSubmitted837),
            Pending277CaClaims = Pending(ClaimStage.Received999),
            Pending835Claims = Pending(ClaimStage.PayerProcessing),
            StuckClaims = ByStatus(ClaimStatus.Stuck),
            ManualActionRequiredClaims = ByStatus(ClaimStatus.ManualActionRequired),
            TopFailureReasons = byFailureCategory
                .Select(x => new CountByKeyDto { Key = ((int)x.Category).ToString(), Label = x.Category.ToString(), Count = x.Count })
                .ToList(),
            ByCustomer = byCustomer
                .Select(x => new CountByKeyDto { Key = x.CustomerCode, Label = string.IsNullOrEmpty(x.CustomerName) ? x.CustomerCode : x.CustomerName, Count = x.Count })
                .ToList(),
            ByAccount = byAccount
                .Select(x => new CountByKeyDto { Key = x.AccountCode, Label = string.IsNullOrEmpty(x.AccountName) ? x.AccountCode : x.AccountName, Count = x.Count })
                .ToList(),
            ByStatus = byStatus
                .Select(x => new CountByKeyDto { Key = ((int)x.Status).ToString(), Label = x.Status.ToString(), Count = x.Count })
                .OrderByDescending(x => x.Count)
                .ToList(),
            ByStage = byStageCounts
                .Select(x => new CountByKeyDto { Key = ((int)x.Stage).ToString(), Label = x.Stage.ToString(), Count = x.Count })
                .OrderBy(x => int.Parse(x.Key))
                .ToList()
        };
    }
}
