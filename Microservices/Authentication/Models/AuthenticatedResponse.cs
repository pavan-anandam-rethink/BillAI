using System.Diagnostics.CodeAnalysis;

namespace Authentication.Models
{
    [ExcludeFromCodeCoverage]
    public class AuthenticatedResponse
    {
        public string Token { get; set; }
        public string? RefreshToken { get; set; }
        /// <summary>Session id for BH master data cache; also embedded in JWT as BillingSessionKey.</summary>
        public string? BillingSessionKey { get; set; }
    }
}
