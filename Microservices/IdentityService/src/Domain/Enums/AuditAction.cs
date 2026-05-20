namespace IdentityService.Domain.Enums;

public enum AuditAction
{
    Login = 0,
    Logout = 1,
    TokenRefresh = 2,
    PasswordChange = 3,
    ProfileUpdate = 4,
    Impersonation = 5
}
