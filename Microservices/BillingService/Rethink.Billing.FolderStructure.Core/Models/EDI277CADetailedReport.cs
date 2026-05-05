namespace Billing.FolderStructure.Core.Models
{
    public class EDI277CADetailedReport
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string TrnReferenceNumber { get; set; }
        public string BhtNumber { get; set; }
        public DateTime ReportDate { get; set; }
        public List<EDI277DetailedReportSummary> Claims { get; set; }
    }
}
