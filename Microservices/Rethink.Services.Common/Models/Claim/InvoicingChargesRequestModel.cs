using System.Collections.Generic;

namespace ReportingService.Web.Models
{
    public class InvoicingChargesRequestModel
    {
        public List<int> ChargeEntryIds { get; set; }
        public int AmountType { get; set; }
    }
}
