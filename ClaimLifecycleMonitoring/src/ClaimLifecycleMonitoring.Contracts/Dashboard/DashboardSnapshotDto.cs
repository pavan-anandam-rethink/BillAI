namespace ClaimLifecycleMonitoring.Contracts.Dashboard;

/// <summary>
/// Aggregate dashboard snapshot for the claim lifecycle monitoring platform.
/// </summary>
public sealed class DashboardSnapshotDto
{
    /// <summary>Timestamp the snapshot was generated (UTC).</summary>
    public DateTime GeneratedUtc { get; init; }

    /// <summary>Total number of claims tracked.</summary>
    public long TotalClaims { get; init; }

    /// <summary>Claims currently progressing.</summary>
    public long InProgressClaims { get; init; }

    /// <summary>Claims that have completed the lifecycle.</summary>
    public long CompletedClaims { get; init; }

    /// <summary>Claims currently in a failed state.</summary>
    public long FailedClaims { get; init; }

    /// <summary>Claims waiting for an external event.</summary>
    public long WaitingClaims { get; init; }

    /// <summary>Claims rejected by validation / clearinghouse / payer.</summary>
    public long RejectedClaims { get; init; }

    /// <summary>Claims with an outstanding balance still due.</summary>
    public long PendingPaymentClaims { get; init; }

    /// <summary>Claims awaiting a 999 acknowledgement.</summary>
    public long Pending999Claims { get; init; }

    /// <summary>Claims awaiting a 277CA acknowledgement.</summary>
    public long Pending277CaClaims { get; init; }

    /// <summary>Claims awaiting an 835 remittance advice.</summary>
    public long Pending835Claims { get; init; }

    /// <summary>Claims currently stuck.</summary>
    public long StuckClaims { get; init; }

    /// <summary>Claims requiring manual action.</summary>
    public long ManualActionRequiredClaims { get; init; }

    /// <summary>Top failure reasons across all claims.</summary>
    public IReadOnlyList<CountByKeyDto> TopFailureReasons { get; init; } = Array.Empty<CountByKeyDto>();

    /// <summary>Claim volume grouped by customer.</summary>
    public IReadOnlyList<CountByKeyDto> ByCustomer { get; init; } = Array.Empty<CountByKeyDto>();

    /// <summary>Claim volume grouped by account.</summary>
    public IReadOnlyList<CountByKeyDto> ByAccount { get; init; } = Array.Empty<CountByKeyDto>();

    /// <summary>Claim volume grouped by status.</summary>
    public IReadOnlyList<CountByKeyDto> ByStatus { get; init; } = Array.Empty<CountByKeyDto>();

    /// <summary>Claim volume grouped by stage.</summary>
    public IReadOnlyList<CountByKeyDto> ByStage { get; init; } = Array.Empty<CountByKeyDto>();
}

/// <summary>Generic key/count pair used across dashboard aggregations.</summary>
public sealed class CountByKeyDto
{
    /// <summary>Aggregation key (customer code, stage name, etc.).</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Human friendly label for the key.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Number of claims in the group.</summary>
    public long Count { get; init; }
}
