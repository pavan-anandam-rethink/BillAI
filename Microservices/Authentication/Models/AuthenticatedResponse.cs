using System.Diagnostics.CodeAnalysis;

namespace Authentication.Models
{
    [ExcludeFromCodeCoverage]
    public class AuthenticatedResponse
    {
        public string Token { get; set; }
        public string? RefreshToken { get; set; }
    }
}
