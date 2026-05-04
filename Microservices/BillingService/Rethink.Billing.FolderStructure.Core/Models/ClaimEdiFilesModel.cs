namespace Billing.FolderStructure.Core.Models
{
    public class ClaimEdiFilesModel
    {
        public int AccountInfoId { get; set; }
        public int MemberId { get; set; }
        public string? FileType { get; set; }          // 837, 999, 277, 835
        public int? ClaimSubmissionId { get; set; }   // 837, 999, 277, 835 
        public string? BatchId { get; set; }
        public int ClaimId { get; set; }             // 999, 277, 835
        public int? PaymentId { get; set; }           // 835 only

        public string? BlobFilePath { get; set; }
    }
}
