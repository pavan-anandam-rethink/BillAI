using ClaimLifecycleMonitoring.Application.Abstractions.Time;
using ClaimLifecycleMonitoring.Contracts.Alerts;
using ClaimLifecycleMonitoring.Contracts.Claims;
using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;

namespace ClaimLifecycleMonitoring.Application.Common.Mapping;

/// <summary>
/// Static mappers from domain entities to <c>Contracts</c> DTOs. Kept as a plain
/// static class to avoid the overhead and configuration burden of a full mapping
/// library. Each method is a pure function safe to invoke from any thread.
/// </summary>
public static class ClaimMappers
{
    /// <summary>Maps a <see cref="Claim"/> to a <see cref="ClaimDto"/>.</summary>
    public static ClaimDto ToDto(this Claim claim, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(clock);
        var now = clock.UtcNow;
        return new ClaimDto
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            CustomerCode = claim.CustomerCode,
            CustomerName = claim.CustomerName,
            AccountCode = claim.AccountCode,
            AccountName = claim.AccountName,
            PatientId = claim.PatientId,
            PatientName = claim.PatientName,
            ProviderId = claim.ProviderId,
            ProviderName = claim.ProviderName,
            CurrentStage = (int)claim.CurrentStage,
            CurrentStageName = claim.CurrentStage.ToString(),
            CurrentStatus = (int)claim.CurrentStatus,
            CurrentStatusName = claim.CurrentStatus.ToString(),
            LastSuccessfulStage = claim.LastSuccessfulStage.HasValue ? (int)claim.LastSuccessfulStage.Value : null,
            LastSuccessfulStageName = claim.LastSuccessfulStage?.ToString(),
            NextExpectedStage = claim.NextExpectedStage.HasValue ? (int)claim.NextExpectedStage.Value : null,
            NextExpectedStageName = claim.NextExpectedStage?.ToString(),
            ClaimAgeHours = Math.Max(0, claim.Age(now).TotalHours),
            SubmissionDateUtc = claim.SubmissionDateUtc,
            LastUpdatedUtc = claim.ModifiedUtc,
            HasFailure = claim.HasFailure,
            FailureCategoryName = claim.FailureCategory.ToString(),
            ExceptionMessage = claim.ExceptionMessage,
            RetryAvailable = claim.RetryAvailable,
            ManualInterventionRequired = claim.ManualInterventionRequired,
            BilledAmount = claim.BilledAmount,
            PaidAmount = claim.PaidAmount
        };
    }

    /// <summary>Maps a <see cref="ClaimLifecycleEvent"/> to a <see cref="ClaimHistoryEntryDto"/>.</summary>
    public static ClaimHistoryEntryDto ToDto(this ClaimLifecycleEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        return new ClaimHistoryEntryDto
        {
            Id = evt.Id,
            Stage = (int)evt.Stage,
            StageName = evt.Stage.ToString(),
            Outcome = (int)evt.Outcome,
            OutcomeName = evt.Outcome.ToString(),
            Message = evt.Message,
            ExceptionDetail = evt.ExceptionDetail,
            OccurredUtc = evt.OccurredUtc
        };
    }

    /// <summary>Maps a <see cref="ClaimAlert"/> to a <see cref="ClaimAlertDto"/>.</summary>
    public static ClaimAlertDto ToDto(this ClaimAlert alert, string claimNumber)
    {
        ArgumentNullException.ThrowIfNull(alert);
        return new ClaimAlertDto
        {
            Id = alert.Id,
            ClaimId = alert.ClaimId,
            ClaimNumber = claimNumber,
            Category = (int)alert.Category,
            CategoryName = alert.Category.ToString(),
            Severity = (int)alert.Severity,
            SeverityName = alert.Severity.ToString(),
            Message = alert.Message,
            StageAtAlert = (int)alert.StageAtAlert,
            StageAtAlertName = alert.StageAtAlert.ToString(),
            RaisedUtc = alert.RaisedUtc,
            Acknowledged = alert.Acknowledged,
            AcknowledgedUtc = alert.AcknowledgedUtc,
            AcknowledgedBy = alert.AcknowledgedBy
        };
    }
}
