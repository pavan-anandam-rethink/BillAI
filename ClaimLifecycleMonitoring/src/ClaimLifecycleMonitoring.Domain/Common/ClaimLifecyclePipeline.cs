using ClaimLifecycleMonitoring.Domain.Enums;

namespace ClaimLifecycleMonitoring.Domain.Common;

/// <summary>
/// Helpers describing the ordering of claim lifecycle stages.
/// </summary>
public static class ClaimLifecyclePipeline
{
    /// <summary>Ordered pipeline of stages a healthy claim traverses.</summary>
    public static readonly IReadOnlyList<ClaimStage> OrderedStages = new[]
    {
        ClaimStage.AppointmentCreated,
        ClaimStage.EligibilityVerification,
        ClaimStage.Authorization,
        ClaimStage.ClaimCreated,
        ClaimStage.ClaimValidation,
        ClaimStage.EdiGenerated837,
        ClaimStage.EdiSubmitted837,
        ClaimStage.Received999,
        ClaimStage.Received277CA,
        ClaimStage.PayerProcessing,
        ClaimStage.Received835,
        ClaimStage.PaymentPosting,
        ClaimStage.PatientInvoice,
        ClaimStage.Completed
    };

    /// <summary>
    /// Returns the next stage that logically follows <paramref name="current"/>.
    /// </summary>
    /// <param name="current">The current stage.</param>
    /// <returns>The next stage or <c>null</c> if <paramref name="current"/> is terminal.</returns>
    public static ClaimStage? NextStage(ClaimStage current)
    {
        var index = OrderedStages.ToList().IndexOf(current);
        if (index < 0 || index >= OrderedStages.Count - 1)
        {
            return null;
        }
        return OrderedStages[index + 1];
    }
}
