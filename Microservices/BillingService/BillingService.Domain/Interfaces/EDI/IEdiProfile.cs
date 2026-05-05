using EdiFabric.Core.Model.Edi.X12;

namespace BillingService.Domain.Interfaces.EDI
{
    public interface IEdiProfile
    {
        string GsVersion { get; }
        string IsaReceiverId { get; }
        string GsReceiverId { get; }

        // Match BuildIsa signature: (string, string, string, string, bool) -> Segment
        ISA BuildIsa(string groupControlNumber, string securityInfo, string submitterId, string isaReceiverId, string testMode);
        GS BuildGs(string groupControlNumber, string customerId, string gsReceiverId, string gsVersion);

    }
}
