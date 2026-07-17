using BillAI.RulesEngine.Domain.Common;

namespace BillAI.RulesEngine.Domain.Entities.Claims;

/// <summary>
/// Represents a healthcare claim validation request and its result.
/// </summary>
public sealed class ClaimValidationRecord : AuditableEntity
{
    private ClaimValidationRecord() { }

    /// <summary>Gets the external claim identifier provided by the caller.</summary>
    public string ClaimId { get; private set; } = string.Empty;

    /// <summary>Gets the serialised claim payload (JSON).</summary>
    public string ClaimJson { get; private set; } = string.Empty;

    /// <summary>Gets whether the claim passed all validation rules.</summary>
    public bool IsPassed { get; private set; }

    /// <summary>Gets the number of rules that passed.</summary>
    public int PassedRuleCount { get; private set; }

    /// <summary>Gets the number of rules that failed.</summary>
    public int FailedRuleCount { get; private set; }

    /// <summary>Gets the number of warnings raised.</summary>
    public int WarningCount { get; private set; }

    /// <summary>Gets the total execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; private set; }

    /// <summary>Gets the serialised validation result (JSON).</summary>
    public string ResultJson { get; private set; } = string.Empty;

    /// <summary>Gets the audit identifier returned to the caller.</summary>
    public Guid AuditId { get; private set; } = Guid.NewGuid();

    public static ClaimValidationRecord Create(
        string tenantId,
        string claimId,
        string claimJson,
        string createdBy)
        => new()
        {
            TenantId = tenantId,
            ClaimId = claimId,
            ClaimJson = claimJson,
            CreatedBy = createdBy
        };

    /// <summary>Records the validation outcome.</summary>
    public void RecordResult(
        bool isPassed,
        int passedRuleCount,
        int failedRuleCount,
        int warningCount,
        long executionTimeMs,
        string resultJson)
    {
        IsPassed = isPassed;
        PassedRuleCount = passedRuleCount;
        FailedRuleCount = failedRuleCount;
        WarningCount = warningCount;
        ExecutionTimeMs = executionTimeMs;
        ResultJson = resultJson;
    }
}
