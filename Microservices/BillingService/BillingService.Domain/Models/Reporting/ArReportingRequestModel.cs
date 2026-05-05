using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models.Reporting
{
    public class ArReportingRequestModel
    {
        public List<int> PayerOrFunderId { get; set; }
        public DateTime? closingDate { get; set; }
    }
}
