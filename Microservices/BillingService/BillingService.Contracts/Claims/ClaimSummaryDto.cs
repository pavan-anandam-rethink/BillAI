namespace BillingService.Contracts.Claims;

/// <summary>
/// Lightweight read-model for a single claim returned by CQRS queries.
/// This DTO lives in the Contracts layer so it can be shared between the Application
/// query handlers and any consumer without coupling to Domain entities.
/// </summary>
public sealed class ClaimSummaryDto
{
    public string ClaimIdentifier { get; init; } = string.Empty;

    public int AccountInfoId { get; init; }

    public string? Status { get; init; }

    public string? ClientName { get; init; }

    public string? FunderName { get; init; }

    public DateTimeOffset? DateCreated { get; init; }

    public DateTimeOffset? DateModified { get; init; }

    /// <summary>Raw JSON payload forwarded from the legacy service for backward compatibility.</summary>
    public object? LegacyPayload { get; init; }
}
