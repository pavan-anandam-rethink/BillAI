namespace Billing.FolderStructure.Core.Models
{
    public class EDI999Summary
    {
        public string FileName { get; set; }
        public string Partner { get; set; }
        public int TotalTransactionSets { get; set; }
        public int Accepted { get; set; }
        public int Rejected { get; set; }
        public int Partial => (Accepted > 0 && Rejected > 0) ? 1 : 0;
        public string Status
        {
            get
            {
                if (Partial == 1) return "Partial";
                if (Rejected == 0 && Accepted > 0) return "Accepted";
                if (Accepted == 0 && Rejected > 0) return "Rejected";
                return "Unknown";
            }
        }
    }
}
