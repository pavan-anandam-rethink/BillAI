namespace Billing.FolderStructure.Core.Models
{
    public class EDI277DetailedReportSummary
    {
        public string ClaimControlNumber { get; set; } 
        public string PatientName { get; set; } 
        public string Status { get; set; } 
        public string StcCode { get; set; } 
        public List<string> StcDescriptions { get; set; } 
        public string ActionRequired { get; set; } 
        public string ClaimTrnNumber { get; set; }
    }
}
