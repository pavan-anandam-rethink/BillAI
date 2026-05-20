namespace IdentityService.Domain.ValueObjects;

public record TokenResult(
    string AccessToken,
    string RefreshToken,
    string? BillingSessionKey);
