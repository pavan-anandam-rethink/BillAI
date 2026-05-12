using ClearingHouse.SharedKernel.Domain;

namespace ClearingHouse.Plugins.Contracts;

/// <summary>
/// Core contract that all clearinghouse plugins must implement.
/// Each plugin is independently deployable and scalable.
/// </summary>
public interface IClearinghousePlugin
{
    string ClearinghouseId { get; }
    string DisplayName { get; }
    bool IsEnabled { get; }

    Task<Result> ValidateConnectionAsync(CancellationToken cancellationToken = default);
    Task<Result<SubmissionResult>> SubmitClaimAsync(ClaimSubmissionRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ResponseFile>>> RetrieveResponsesAsync(RetrieveResponsesRequest request, CancellationToken cancellationToken = default);
    Task<Result<EligibilityResponse>> CheckEligibilityAsync(EligibilityRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteRemoteFileAsync(string filePath, CancellationToken cancellationToken = default);
}

public record ClaimSubmissionRequest(
    string EdiContent,
    string FileName,
    string CorrelationId,
    IDictionary<string, string>? Metadata = null);

public record SubmissionResult(
    bool IsSuccess,
    string? TransactionId,
    string? RemotePath,
    string? ErrorMessage);

public record RetrieveResponsesRequest(
    string CorrelationId,
    string? FilePattern = null,
    DateTime? Since = null);

public record ResponseFile(
    string FileName,
    string RemotePath,
    Stream Content,
    long SizeBytes,
    DateTime LastModified);

public record EligibilityRequest(
    string SubscriberId,
    string ProviderId,
    string PayerId,
    DateTime EffectiveDate,
    string CorrelationId);

public record EligibilityResponse(
    bool IsEligible,
    string? StatusCode,
    string? StatusMessage,
    IDictionary<string, string>? Benefits = null);
