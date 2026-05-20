namespace IdentityService.Application.DTOs;

public class AuthenticatedResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? BillingSessionKey { get; set; }
}
