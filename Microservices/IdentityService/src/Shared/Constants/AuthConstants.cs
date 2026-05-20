namespace IdentityService.Shared.Constants;

public static class AuthConstants
{
    public const double JwtExpiryDurationMinutes = 5;
    public const string DefaultJwtIssuer = "Rethink Billing";

    public static class ClaimTypes
    {
        public const string AccountInfoId = "AccountInfoId";
        public const string MemberId = "MemberId";
        public const string MemberName = "MemberName";
        public const string MemberRole = "MemberRole";
        public const string OsbEnabled = "OsbEnabled";
        public const string ImpersonatedUser = "ImpersonatedUser";
        public const string ImpersonationUserName = "ImpersonationUserName";
        public const string ImpersonationUserEmail = "ImpersonationUserEmail";
        public const string AccountDetail = "AccountDetail";
        public const string BillingSessionKey = "BillingSessionKey";
        public const string Permissions = "Permissions";
    }

    public static class OsbPermissions
    {
        public const string BillingView = "BillingView";
        public const string BillingPostPayments = "BillingPostPayments";
        public const string BillingReopenEncounter = "BillingReopenEncounter";
        public const string BillingCloseEncounters = "BillingCloseEncounters";
        public const string BillingClientHistory = "BillingClientHistory";
    }
}
