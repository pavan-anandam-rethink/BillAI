using Rethink.Services.Common.Models;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class GetPatientPaymentServiceLinesModel : ListSortFilterModel
    {
        public int PaymentId { get; set; }
        public int PatientId { get; set; }
        public bool ShowPaid { get; set; }
        public bool IsLinked { get; set; }
    }
}
