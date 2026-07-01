namespace ClaimLifecycleMonitoring.Contracts.Claims;

/// <summary>
/// Query parameters used when searching / paging over claims.
/// </summary>
public sealed class ClaimSearchQuery
{
    /// <summary>Zero-based page index.</summary>
    public int Page { get; set; } = 0;

    /// <summary>Page size (1-500).</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Free text term matched against claim number, patient or provider name.</summary>
    public string? SearchTerm { get; set; }

    /// <summary>Optional customer code filter.</summary>
    public string? CustomerCode { get; set; }

    /// <summary>Optional account code filter.</summary>
    public string? AccountCode { get; set; }

    /// <summary>Optional current-stage filter (numeric enum value).</summary>
    public int? CurrentStage { get; set; }

    /// <summary>Optional current-status filter (numeric enum value).</summary>
    public int? CurrentStatus { get; set; }

    /// <summary>Optional failure category filter (numeric enum value).</summary>
    public int? FailureCategory { get; set; }

    /// <summary>Show only claims requiring manual intervention.</summary>
    public bool? ManualInterventionRequired { get; set; }

    /// <summary>Show only claims currently flagged as stuck.</summary>
    public bool? Stuck { get; set; }
}
