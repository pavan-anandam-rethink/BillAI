namespace IdentityService.Application.DTOs;

public class AuthenticateRequestDto
{
    public string AccountInfoId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string MemberRole { get; set; } = string.Empty;
    public string ImpersonationUserObjectId { get; set; } = string.Empty;
    public string ImpersonationUserName { get; set; } = string.Empty;
    public string ImpersonationUserEmail { get; set; } = string.Empty;
    public string BillingSessionKey { get; set; } = string.Empty;
    public Dictionary<string, bool> Permissions { get; set; } = [];
}
