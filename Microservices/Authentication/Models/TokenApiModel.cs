using System.Diagnostics.CodeAnalysis;

namespace Authentication.Models
{
    [ExcludeFromCodeCoverage]
    public class TokenApiModel
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
