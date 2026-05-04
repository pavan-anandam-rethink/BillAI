using System.Collections.Generic;

namespace BillingService.Domain.Models.Claims.WriteOff
{
    public class EditChargeEntryWriteOffModelWithUserInfo : UserInfo
    {
        public int ClaimId { get; set; }
        public List<WriteOffDetailsModel> WriteOffDetails { get; set; }

    }

    public class WriteOffDetailsModel
    {
        public int ChargeEntryWriteOffId { get; set; }
        public decimal WriteOffAmount { get; set; }
        public int WriteOffReasonCodeId { get; set; }
    }

    public class WriteOffReasonCodDescriptionModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }
}
