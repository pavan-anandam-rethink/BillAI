namespace BillAI.RulesEngine.Shared.Constants;

/// <summary>
/// Tenant and system-level constant identifiers.
/// </summary>
public static class SystemConstants
{
    /// <summary>The system user identifier used for automated operations.</summary>
    public const string SystemUser = "system";

    /// <summary>Default tenant identifier for single-tenant deployments.</summary>
    public const string DefaultTenantId = "default";
}

/// <summary>
/// Rule status constants.
/// </summary>
public static class RuleStatusConstants
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Archived = "Archived";
}

/// <summary>
/// Workflow status constants.
/// </summary>
public static class WorkflowStatusConstants
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Archived = "Archived";
}

/// <summary>
/// Workflow execution status constants.
/// </summary>
public static class ExecutionStatusConstants
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Suspended = "Suspended";
    public const string Cancelled = "Cancelled";
    public const string TimedOut = "TimedOut";
}
