using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClaimLifecycleMonitoring.Persistence.Seeding;

/// <summary>
/// Seeds baseline configuration (per-stage SLAs) and a small set of demo
/// claims when the database is empty.
/// </summary>
public static class ClaimLifecycleSeeder
{
    /// <summary>Applies seed data to the database if it has not yet been seeded.</summary>
    public static async Task SeedAsync(ClaimLifecycleDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        await SeedSlaConfigurationsAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await SeedDemoClaimsAsync(db, logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedSlaConfigurationsAsync(ClaimLifecycleDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.StageSlaConfigurations.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var defaults = new (ClaimStage Stage, int SlaHours, bool ExpectsInbound, FailureCategory Category)[]
        {
            (ClaimStage.AppointmentCreated, 2, false, FailureCategory.SlaBreach),
            (ClaimStage.EligibilityVerification, 4, false, FailureCategory.EligibilityFailure),
            (ClaimStage.Authorization, 24, false, FailureCategory.AuthorizationFailure),
            (ClaimStage.ClaimCreated, 4, false, FailureCategory.SlaBreach),
            (ClaimStage.ClaimValidation, 2, false, FailureCategory.ValidationFailure),
            (ClaimStage.EdiGenerated837, 2, false, FailureCategory.EdiGenerationFailure),
            (ClaimStage.EdiSubmitted837, 24, true, FailureCategory.Missing999),
            (ClaimStage.Received999, 72, true, FailureCategory.Missing277CA),
            (ClaimStage.Received277CA, 720, false, FailureCategory.SlaBreach),
            (ClaimStage.PayerProcessing, 720, true, FailureCategory.Missing835),
            (ClaimStage.Received835, 24, false, FailureCategory.PaymentPostingFailure),
            (ClaimStage.PaymentPosting, 24, false, FailureCategory.PaymentPostingFailure),
            (ClaimStage.PatientInvoice, 48, false, FailureCategory.InvoiceGenerationFailure),
            (ClaimStage.Completed, 24, false, FailureCategory.SlaBreach)
        };

        foreach (var d in defaults)
        {
            db.StageSlaConfigurations.Add(new StageSlaConfiguration
            {
                Stage = d.Stage,
                SlaHours = d.SlaHours,
                ExpectsInboundMessage = d.ExpectsInbound,
                BreachCategory = d.Category
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Seeded {Count} stage SLA configurations.", defaults.Length);
    }

    private static async Task SeedDemoClaimsAsync(ClaimLifecycleDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.Claims.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var demoClaims = new[]
        {
            Claim.Create("CLM-1001", "ACME", "Acme Health", "PAY-BCBS", "Blue Cross Blue Shield",
                "PAT-001", "Jane Doe", "PROV-01", "Dr. Alice Smith", 1250.75m, 24, now.AddDays(-1)),
            Claim.Create("CLM-1002", "ACME", "Acme Health", "PAY-AETNA", "Aetna",
                "PAT-002", "John Roe", "PROV-02", "Dr. Bob Jones", 850.00m, 24, now.AddDays(-3)),
            Claim.Create("CLM-1003", "SUMMIT", "Summit Clinics", "PAY-UHC", "United Healthcare",
                "PAT-003", "Mary Poppins", "PROV-03", "Dr. Carla Diaz", 4200.00m, 24, now.AddDays(-7))
        };

        db.Claims.AddRange(demoClaims);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Seeded {Count} demo claims.", demoClaims.Length);
    }
}
