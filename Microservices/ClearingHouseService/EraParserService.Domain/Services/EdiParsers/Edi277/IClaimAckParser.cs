using EdiFabric.Core.Model.Edi;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Handlers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EraParserService.Domain.Services.EdiParsers.Edi277
{
    public interface IClaimAckParser
    {
        Task ParseAsync(EdiDownloadData ediDownloadata, List<IEdiItem> ediItems, byte[] data, ClaimSubmissionEntity claimSubmission, string fileName);
    }
}
