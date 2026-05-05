using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class PatientServiceLinesModel
    {
        public int PatientId { get; set; }
        public List<ServiceLinePostDeleteModel> ServiceLines { get; set; }
    }
}
