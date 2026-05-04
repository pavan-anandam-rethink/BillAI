using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Files;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using System;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Files
{
    public class EdiFilesDownload : IEdiFilesDownload
    {
        private readonly IBillingFilePath _billingFilePath;
        private readonly ILogger<EdiFilesDownload> _logger;
        public EdiFilesDownload(IBillingFilePath billingFilePath, ILogger<EdiFilesDownload> logger)
        {
            _billingFilePath = billingFilePath;
            _logger = logger;
        }


        public async Task<bool> SaveClaimEdiFilePath(BillingRequest billingRequest, string fullFilePath, ClaimSubmissionEntity claimSubmission = null)
        {
            try
            {
                _logger.LogInformation(
                    "{Service}.{Method} - Beginning save of claim EDI file path. AccountInfoId={AccountInfoId}, FilePath={FilePath}",
                    nameof(EdiFilesDownload), nameof(SaveClaimEdiFilePath), billingRequest.AccountInfoId, fullFilePath);

                var parts = billingRequest.FieldIdentifier.Split('/');
                var ediFileType = billingRequest.Data != null && billingRequest.Data.Length > 0
                    ? await _billingFilePath.GetEdiFileType(billingRequest) : "Unknown";

                ediFileType = ediFileType == "Unknown" ? parts[1] : ediFileType;

                var claimEdiFilesModel = new ClaimEdiFilesModel
                {
                    AccountInfoId = billingRequest.AccountInfoId ?? 0,
                    MemberId = claimSubmission != null ? claimSubmission.Claim.MemberId : 0,
                    FileType = ediFileType,
                    ClaimSubmissionId = claimSubmission != null ? claimSubmission.Id : null,
                    ClaimId = claimSubmission != null ? claimSubmission.Claim.Id : 0,
                    BlobFilePath = fullFilePath,
                    PaymentId = billingRequest.PaymentId
                };

                await _billingFilePath.AddOrUpdateBlobFilePath(claimEdiFilesModel);

                _logger.LogInformation(
                    "{Service}.{Method} - Successfully saved claim EDI file path. AccountInfoId={AccountInfoId}, FileType={FileType}",
                    nameof(EdiFilesDownload), nameof(SaveClaimEdiFilePath), billingRequest.AccountInfoId, ediFileType);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Service}.{Method} - Error saving claim EDI file path. AccountInfoId={AccountInfoId}, FilePath={FilePath}",
                    nameof(EdiFilesDownload), nameof(SaveClaimEdiFilePath), billingRequest.AccountInfoId, fullFilePath);

                return false;
            }
        }
    }
}
