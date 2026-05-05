using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models.Reporting
{
    public class PayAdjReportingRequestModel
    {
        public List<int> FunderId { get; set; }
        public int RangeType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
