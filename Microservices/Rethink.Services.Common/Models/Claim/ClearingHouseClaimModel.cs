using Rethink.Services.Common.Enums.Billing;
using System.Collections.Generic;
using System.ComponentModel;

namespace Rethink.Services.Common.Models.Claim
{
    public class ClearingHouseClaimModel
    {
        public int claimId { get; set; }
        public int clearinghouseId { get; set; }
        [DefaultValue(false)]
        public bool isSecondary { get; set; } = false;
        public AdjustmentLevel? AdjustmentLevel { get; set; }
    }

    public class ERAUploadModel
    {
        public int accountInfoId { get; set; }
        public List<int> PaymentIds { get; set; }
        public byte[] data { get; set; }
        public string fileName { get; set; }
        public string containerName { get; set; }
        public string claimIdentifier { get; set; }

    }
}