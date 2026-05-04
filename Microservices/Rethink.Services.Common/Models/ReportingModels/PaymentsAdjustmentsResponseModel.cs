using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class PaymentsAdjustmentsResponseModel
    {
        public List<PaymentsAdjustmentsResponse> paymentsAdjustments { get; set; }
        public int totalCount { get; set; }
    }

}
