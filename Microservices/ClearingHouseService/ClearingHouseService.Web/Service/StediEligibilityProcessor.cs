using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Billing;
using ClearingHouseService.Web.Interface;
using Rethink.Services.Common.Dtos.ClearingHouse;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.EligibilityRequest;
using System.Text;

namespace ClearingHouseService.Web.Service
{
    public sealed class StediEligibilityProcessor : IStediEligibilityProcessor
    {
        private readonly IStediEligibilityClient _stediClient;
        private readonly IBillingBlobService _blobService;
        private readonly IX12Parser<Eligibility271ParsedResponse> _parser;
        private readonly IEligibility271Repository _repository;
        private readonly IBillingFilePath _billingFilePath;
        private readonly IBillingBlobService _billingBlobService;
        private readonly ICHService _blobBackupService;
        private readonly IConfiguration _configuration;

        public StediEligibilityProcessor(IStediEligibilityClient stediClient,
                                        IBillingBlobService blobService,
                                        IX12Parser<Eligibility271ParsedResponse> parser,
                                        IEligibility271Repository repository,
                                        IBillingFilePath billingFilePath,
                                        IBillingBlobService billingBlobService,
                                        ICHService blobBackupService,
                                        IConfiguration configuration)
        {
            _stediClient = stediClient;
            _blobService = blobService;
            _parser = parser;
            _repository = repository;
            _billingFilePath = billingFilePath;
            _billingBlobService = billingBlobService;
            _blobBackupService = blobBackupService;
            _configuration = configuration;
        }

        /// <summary>
        /// uploads 270 to blob, submits to Stedi, uploads 271 to blob, parses and saves to repository(DB)
        /// </summary>

        public async Task ProcessAsync(StediEligibilityJobDTO job, CancellationToken cancellationToken)
        {
            var controlNumber = job.FunderId?.ToString().TrimStart('0') != null ? int.Parse(job.FunderId?.ToString().TrimStart('0')) : (int?)null;
            var fileName = $"{controlNumber}_{DateTime.UtcNow:yyMMdd}.edi";
            byte[] edi270Data = Encoding.UTF8.GetBytes(job.Edi270Request);
            var billingRequest = new BillingFolderStructureModel
            {
                FileName = fileName,
                AccountInfoId = job.AccountId,
                Data = edi270Data,
                TransactionNumber = controlNumber,
                ClearingHouseTitle = BillingClearingHousesEnum.Stedi.ToString()
            };

            await UploadIntoBillingBlob(billingRequest, BlobFolderNames.Outgoing, null);
            var edi271Result = await _stediClient.Submit270Async(job.Edi270Request, cancellationToken);
            if (!edi271Result.IsSuccess)
            {
                await _repository.SaveAsync(MapDto(job, edi271Result), cancellationToken);
                await UploadIntoBillingBlob(billingRequest, BlobFolderNames.Failed, null);
                billingRequest.Message = ($"Error validating 270 file - for FunderId '{controlNumber}' Error={edi271Result.FailureResponse} \nTimestamp: {DateTime.UtcNow:G}");
                await UploadIntoBillingBlob(billingRequest, BlobFolderNames.Logs, BlobFolderNames.ErrorLogs);

                return;
            }

            await UploadIntoBillingBlob(billingRequest, BlobFolderNames.Archive, null);
            billingRequest.Message = ($"Success validating 270 file - for FunderId '{controlNumber}'\nTimestamp: {DateTime.UtcNow:G}");
            await UploadIntoBillingBlob(billingRequest, BlobFolderNames.Logs, BlobFolderNames.ProcessingLogs);
            // Parsing 271 
            billingRequest.Data = Encoding.UTF8.GetBytes(edi271Result.X12Response);
            await UploadIntoBillingBlob(billingRequest, BlobFolderNames.Incoming, null);

            try
            {
                var parsed = _parser.Parse(edi271Result.X12Response);
                await _repository.SaveAsync(MapDto(job, parsed), cancellationToken);

                await UploadIntoBillingBlob(billingRequest, BlobFolderNames.Archive, null);
                billingRequest.Message = ($"Successfully processed 271 file - for FunderId '{controlNumber}' \nTimestamp: {DateTime.UtcNow:G}");
                await UploadIntoBillingBlob(billingRequest, BlobFolderNames.Logs, BlobFolderNames.ProcessingLogs);
            }
            catch (Exception ex)
            {
                var failureResponse = new Eligibility271ParsedResponse
                {
                    IsSuccess = false,
                    FailureResponse = $"Failed to parse 271 response: {ex.Message}"
                };

                await UploadIntoBillingBlob(billingRequest, BlobFolderNames.Failed, null);
                billingRequest.Message = ($"Error validating 271 file - for FunderId '{controlNumber}' Error={ex.Message} \nTimestamp: {DateTime.UtcNow:G}");
                await UploadIntoBillingBlob(billingRequest, BlobFolderNames.Logs, BlobFolderNames.ErrorLogs);
                await _repository.SaveAsync(MapDto(job, failureResponse), cancellationToken);
            }
        }

        private static Eligibility271ResponseEntity MapDto(StediEligibilityJobDTO job, Eligibility271ParsedResponse parsed)
        {
            return new Eligibility271ResponseEntity
            {
                FunderId = job.FunderId,
                AccountId = job.AccountId,
                CreatedBy = job.MemberId,
                CreatedDate = job.EffectiveDate,
                CoverageStatus = parsed.CoverageStatus,
                EffectiveStartDate = parsed.EffectiveStartDate,
                EffectiveEndDate = parsed.EffectiveEndDate,
                SubscriberStartDate = parsed.SubscriberStartDate,
                SubscriberEndDate = parsed.SubscriberEndDate,
                TransactionControlNumber = job.CorrelationId,
                PlanStartDate = parsed.PlanStartDate,
                PlanEndDate = parsed.PlanEndDate,
                FailureResponse = parsed.IsSuccess ? null : parsed.FailureResponse,
            };
        }

        private async Task UploadIntoBillingBlob(BillingFolderStructureModel model, BlobFolderNames folderName, BlobFolderNames? subFolderName)
        {
            var result = new ClaimSubmissionEntity();
            var dataString = System.Text.Encoding.UTF8.GetString(model.Data);
            var transactionControlResult = await _billingFilePath.GetTransactionControlNumber(dataString);
            var fileType = (FileTypes)int.Parse(transactionControlResult.FileType);
            if (fileType != FileTypes.Type270 && fileType != FileTypes.Type271)
            {
                result = await _billingFilePath.FetchClaimSubmissionDataForERA(transactionControlResult);
            }

            var billingRequest = new BillingRequest
            {
                FieldIdentifier = model.FileName,
                FolderName = folderName.ToString(),
                AccountInfoId = result.Claim != null ? result.Claim.AccountInfoId : model.AccountInfoId,
                Data = model.Data,
                BillingContainerName = BillingConstants.BillingContainerName,
                TransactionNumber = result.Id > 0 ? result.Id : model.TransactionNumber,
                SubFolderName = subFolderName.ToString(),
                ClearingHouseTitle = model.ClearingHouseId > 0 ? Enum.GetName(typeof(BillingClearingHousesEnum), model.ClearingHouseId) : model.ClearingHouseTitle
            };

            var billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            var modelData = model.Data;
            if (subFolderName != null) model.Data = Encoding.UTF8.GetBytes(model.Message);
            var newName = await _billingBlobService.UploadIntoContainerAsync(BillingConstants.BillingContainerName, fullFilePath, new MemoryStream(model.Data));
            model.FileName = newName;
            model.Data = modelData;
            if (!string.IsNullOrEmpty(billingRequest.SubFolderName))
            {
                if (fileType == FileTypes.Type270 && billingRequest.FolderName != BlobFolderNames.Outgoing.ToString())
                {
                    billingRequest.FolderName = BlobFolderNames.Outgoing.ToString();
                }
                else if (fileType == FileTypes.Type271 && billingRequest.FolderName != BlobFolderNames.Incoming.ToString())
                {
                    billingRequest.FolderName = BlobFolderNames.Incoming.ToString();
                }

                billingRequest.SubFolderName = null;
                billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
                (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
                await (string.IsNullOrEmpty(billingRequest.SubFolderName) ? _billingBlobService.DeleteBlobFromContainerAsync(BillingConstants.BillingContainerName, fullFilePath) : Task.CompletedTask);
            }
        }
    }
}
