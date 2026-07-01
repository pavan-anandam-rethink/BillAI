using ClaimLifecycleMonitoring.Domain.Common;
using ClaimLifecycleMonitoring.Domain.Enums;

namespace ClaimLifecycleMonitoring.Domain.Entities;

/// <summary>
/// Configurable per-stage SLA (in hours) after which a claim is considered stuck.
/// </summary>
public sealed class StageSlaConfiguration : EntityBase
{
    /// <summary>The stage this SLA applies to.</summary>
    public ClaimStage Stage { get; set; }

    /// <summary>Maximum permitted duration in the stage in hours before an alert is raised.</summary>
    public int SlaHours { get; set; }

    /// <summary>Whether missing an inbound message (999/277CA/835) at this stage is a failure.</summary>
    public bool ExpectsInboundMessage { get; set; }

    /// <summary>Failure category to apply when the SLA is breached at this stage.</summary>
    public FailureCategory BreachCategory { get; set; } = FailureCategory.SlaBreach;
}
