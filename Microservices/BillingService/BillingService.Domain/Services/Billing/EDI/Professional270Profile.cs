using BillingService.Domain.Interfaces.EDI;
using EdiFabric.Core.Model.Edi.X12;
using Rethink.Services.Common.Enums.Billing;

namespace BillingService.Domain.Services.Billing.EDI
{
    public class Professional270Profile : IEdiProfile
    {
        public string GsVersion => "005010X279A1";
        public string IsaReceiverId { get; }
        public string GsReceiverId { get; }

        public Professional270Profile(string isaReceiverId, string gsReceiverId)
        {
            IsaReceiverId = isaReceiverId;
            GsReceiverId = gsReceiverId;
        }

        public ISA BuildIsa(string groupControlNumber, string securityInfo, string submitterId, string isaReceiverId, string testMode)
            => SegmentBuilders.BuildIsa(groupControlNumber, securityInfo, submitterId, isaReceiverId, testMode);

        public GS BuildGs(string groupControlNumber, string customerId, string gsReceiverId, string gsVersion)
            => SegmentBuilders.BuildGs(groupControlNumber, customerId, gsReceiverId, gsVersion);
    }
}
