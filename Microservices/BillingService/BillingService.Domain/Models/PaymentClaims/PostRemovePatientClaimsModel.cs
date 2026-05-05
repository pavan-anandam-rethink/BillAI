using Rethink.Services.Common.Enums.Billing;
using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class PostRemovePatientClaimsModel : UserInfo
    {
        public int PaymentId { get; set; }
        public List<PatientServiceLinesModel> PatientServiceLines { get; set; }
        public BulkPostingCriteria PostingCriteriaId { get; set; }
    }
}
