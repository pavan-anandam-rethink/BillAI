using System.Collections.Generic;

namespace BillingService.Domain.Models
{
    public class ClaimNextFundersAndControlNumberModel
    {
        public List<ClaimPatientFunderModel> funders { get; set; }
        public string controlNumber { get; set; }
    }
}
