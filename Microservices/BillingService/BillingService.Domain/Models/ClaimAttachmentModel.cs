using System;

namespace BillingService.Domain.Models
{
    public class ClaimAttachmentModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public double FileSize { get; set; }
        public string FileMimeType { get; set; }
        public string Notes { get; set; }
        public string FilePath { get; set; }
        public DateTime DateCreated { get; set; }
        public int ClaimId { get; set; }
        public string FileLink { get; set; }
    }
}