using Billing.FolderStructure.Core.Models;
using Rethink.Services.Common.Entities.Billing.Claim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Files
{
    public interface IEdiFilesDownload
    {
        Task<bool> SaveClaimEdiFilePath(BillingRequest billingRequest, string fullFilePath, ClaimSubmissionEntity? claimSubmission = null);
    }
}
