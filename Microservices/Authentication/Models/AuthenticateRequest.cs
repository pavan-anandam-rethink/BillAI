
using System.Diagnostics.CodeAnalysis;

namespace Authentication.Models
{
    [ExcludeFromCodeCoverage]
    public class AuthenticateRequest
    {
        public string AccountInfoId { get; set; }
        public string MemberId { get; set; }
        public string MemberName { get; set; }
        public string MemberRole { get; set; }
        public string ImpersonationUserObjectId { get; set; } = string.Empty;
        public string ImpersonationUserName { get; set; } = string.Empty;
        public string ImpersonationUserEmail { get; set; } = string.Empty;
        /// <summary>Opaque key for Redis session cache of BH master data (set at login).</summary>
        public string BillingSessionKey { get; set; } = string.Empty;
        public Dictionary<string, bool> Permissions { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Token
    {
        public string token { get; set; }
    }
}