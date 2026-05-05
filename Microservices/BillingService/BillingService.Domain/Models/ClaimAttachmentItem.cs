using Rethink.Services.Common.Entities.Billing.Claim;
using System;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class ClaimAttachmentItem
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
        public void UpdateEntity(ClaimAttachmentEntity entity)
        {
            entity.FileName = FileName;
            entity.FileSize = FileSize;
            entity.FileMimeType = FileMimeType;
            entity.Notes = Notes;
            entity.FilePath = FilePath;
        }
    }
}