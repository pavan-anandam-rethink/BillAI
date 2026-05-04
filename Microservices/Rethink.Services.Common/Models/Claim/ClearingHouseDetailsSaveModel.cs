using Rethink.Services.Common.Enums.Billing;
using System;

namespace Rethink.Services.Common.Models.Claim
{
    public class ClearingHouseDetailsSaveModel
    {
        public int clearingHouseId { get; set; }
        public int submissionId { get; set; }
        public int claimId { get; set; }
        public int validationErrorId { get; set; }
        public ClaimResponseFileType fileTypeId { get; set; }
        public string batchId { get; set; }
        public int memberId { get; set; }
        public bool isAccepted { get; set; }
        public string fileIdentifier { get; set; }
        public DateTime downloadDateTime { get; set; }
    }
}
