using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class ClaimFilterOptionModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ClaimClientFilterOptionModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}