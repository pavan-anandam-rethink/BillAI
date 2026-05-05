using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class ClaimDeleteResultModel
    {
        public int Id { get; set; }
        public string ClaimIdentifier { get; set; }
        public IEnumerable<int> AppointmentIds { get; set; }
    }
}