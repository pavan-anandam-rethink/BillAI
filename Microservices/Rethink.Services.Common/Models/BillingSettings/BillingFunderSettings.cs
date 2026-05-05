using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Models.BillingSettings
{
    [ExcludeFromCodeCoverage]
    public class BillingFunderSettings
    {
        public string ClaimFilingIndicator { get; set; } = "ZZ";

        public bool IncludeTaxonomyCode { get; set; } = false;

        public int AuthorizationStatus { get; set; } = 1;
    }
}
