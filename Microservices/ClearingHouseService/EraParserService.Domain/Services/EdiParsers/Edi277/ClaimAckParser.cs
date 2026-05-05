using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Files;
using EdiFabric.Core.Model.Edi;
using EdiFabric.Templates.Hipaa5010;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EraParserService.Domain.Services.EdiParsers.Edi277
{
    public class ClaimAckParser : BaseService, IClaimAckParser
    {
        private readonly IRepository<BillingDbContext, PaymentEraUploadEntity> _paymentEraUploadRepository;
        private readonly IBillingBlobService _billingBlobService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IRepository<BillingDbContext, ClaimValidationErrorEntity> _claimValidationErrorRepository;
        private readonly IRepository<BillingDbContext, ClaimErrorMessageEntity> _claimErrorMessageRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _claimSubmissionRepository;
        private readonly IRepository<BillingDbContext, ExternalCodeEntity> _externalCodeRepository;
        private readonly IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity> _clearingHouseResponseRepository;
        private readonly IBaseClaimService _claimService;
        private readonly List<string> _claimStatusCategoryRejectCodes = new List<string> { "A3", "A4", "A5", "A6", "A7", "A8" };
        private readonly List<string> _claimStatusCategoryIgnoreCodes = new List<string> { "A0", "A1" };
        private readonly string _claimStatusCategoryAcceptCode = "A2";
        private readonly IBillingFilePath _billingFilePath;
        private readonly IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> _claimSubmissionFunderSequence;
        private readonly IEdiFilesDownload _ediFilesDownload;

        public ClaimAckParser(
            IRepository<BillingDbContext, PaymentEraUploadEntity> paymentEraUploadRepository,
            IBillingBlobService billingBlobService,
            ILoggerFactory loggerFactory,
            IRepository<BillingDbContext, ClaimValidationErrorEntity> claimValidationErrorRepository,
            IRepository<BillingDbContext, ClaimErrorMessageEntity> claimErrorMessageRepository,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, ClaimSubmissionEntity> claimSubmissionRepository,
            IRepository<BillingDbContext, ExternalCodeEntity> externalCodeRepository,
            IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity> clearingHouseResponseRepository,
            IBaseClaimService claimService,
            IBillingFilePath billingFilePath,
            IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> claimSubmissionFunderSequence,
            IEdiFilesDownload ediFilesDownload)
        {
            _paymentEraUploadRepository = paymentEraUploadRepository;
            _billingBlobService = billingBlobService;
            _loggerFactory = loggerFactory;
            _claimValidationErrorRepository = claimValidationErrorRepository;
            _claimErrorMessageRepository = claimErrorMessageRepository;
            _claimRepository = claimRepository;
            _claimSubmissionRepository = claimSubmissionRepository;
            _externalCodeRepository = externalCodeRepository;
            _clearingHouseResponseRepository = clearingHouseResponseRepository;
            _claimService = claimService;
            _logger = _loggerFactory.CreateLogger(GetType());
            _billingFilePath = billingFilePath;
            _claimSubmissionFunderSequence = claimSubmissionFunderSequence;
            _ediFilesDownload = ediFilesDownload;
        }

        public async Task ParseAsync(EdiDownloadData ediDownloadData, List<IEdiItem> ediItems, byte[] data, ClaimSubmissionEntity claimSubmission, string fileIdentifier)
        {
            var parts = fileIdentifier.Split('/');
            string fileName = parts[^1];
            string partner = parts[2];
            int accountInfoId = claimSubmission != null
                        ? claimSubmission.Claim.AccountInfoId
                        : 0;

            var billingRequest = new BillingRequest
            {
                FieldIdentifier = fileName,
                FolderName = BlobFolderNames.Reports.ToString(),
                AccountInfoId = accountInfoId,
                Data = data,
                BillingContainerName = BillingConstants.BillingContainerName,
                TransactionNumber = claimSubmission?.Id,
                SubFolderName = BlobFolderNames.Detailed.ToString(),
                ClearingHouseTitle = Enum.GetName(typeof(BillingClearingHousesEnum), ediDownloadData.ClearingHouseId),
                ClearingHouseId = ediDownloadData.ClearingHouseId
            };

            var billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            var response = await _billingBlobService.Update277DetailedReportAsync(BillingConstants.BillingContainerName, ediDownloadData.EdiData, fullFilePath);

            billingRequest.SubFolderName = BlobFolderNames.Daily.ToString();
            billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            await _billingBlobService.Update277DailySummaryReportAsync(BillingConstants.BillingContainerName, ediDownloadData.EdiData, fullFilePath, response);

            var is277AlreadyProcessed = _clearingHouseResponseRepository.Query().Where(x => x.FileIdentifier == ediDownloadData.FileIdentifier).Any();
            if (is277AlreadyProcessed) return;

            _logger.LogInformation($"[{DateTime.Now:G}]: Parsing 277 file: {fileName} ");

            var hcClaimAckList = ediItems.OfType<TS277A>().ToList();
            foreach (var claimAcknowledgment in hcClaimAckList)
            {
                var billingProviderServiceLoop = claimAcknowledgment.Loop2000A.Loop2000B.Loop2000C.FirstOrDefault();
                var patientLoop = billingProviderServiceLoop.Loop2000D;
                if (patientLoop != null && patientLoop.Any())
                {
                    foreach (var patient in patientLoop)
                    {
                        //patient claims
                        var claimStatusLoop = patient.Loop2200D;
                        foreach (var claimStatus in claimStatusLoop)
                        {
                            await HandleClaimStatus(claimStatus, ediDownloadData, data, claimSubmission.Claim.AccountInfoId, fileIdentifier);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation($"[{DateTime.Now:G}]: Patient loop is missing in 277 file : {fileName}");
                }

            }
        }


        private async Task HandleClaimStatus(Loop_2200D_277A claimStatus, EdiDownloadData ediDownloadData, byte[] data, int? accounInfoId, string fileIdentifier)
        {
            var isClaimRejected = claimStatus.STC_ClaimLevelStatusInformation
                .Any(x => _claimStatusCategoryRejectCodes.Contains(x.HealthCareClaimStatus_01.HealthCareClaimStatusCategoryCode_01.Split(":")[0]));

            if (isClaimRejected)
            {
                await HandleRejectStatus(claimStatus, ediDownloadData, data, accounInfoId, fileIdentifier);
            }
            else
            {
                await HandleAcceptStatus(claimStatus, ediDownloadData, data, accounInfoId, fileIdentifier);
            }
        }

        private async Task HandleRejectStatus(Loop_2200D_277A claimStatusLoop, EdiDownloadData ediDownloadData, byte[] data, int? accounInfoId, string fileIdentifier)
        {
            var submissionIdentifier = claimStatusLoop.TRN_ClaimStatusTrackingNumber.CurrentTransactionTraceNumber_02;
            var claimStatusInfoLoop = claimStatusLoop.STC_ClaimLevelStatusInformation;

            var billingBlobModel = new BillingFolderStructureModel
            {
                Data = data,
                FileName = fileIdentifier,
                AccountInfoId = accounInfoId,
                ClearingHouseId = ediDownloadData.ClearingHouseId
            };

            var submissionEntity = _claimSubmissionRepository.Query().Include(x => x.Claim).FirstOrDefault(x => x.ClaimSubmissionIdentifier == submissionIdentifier);
            if (submissionEntity != null)
            {
                int refValidationId = 0;
                var isCodePresent = false;
                var claimStatusCodes = await _externalCodeRepository.Query().Where(x => x.CodeTypeId == ExternalCodeType.ClaimStatusCode).ToListAsync();
                var claimStatusCategoryCodes = await _externalCodeRepository.Query().Where(x => x.CodeTypeId == ExternalCodeType.ClaimStatusCategoryCode).ToListAsync();
                foreach (var claimStatusInfo in claimStatusInfoLoop)
                {
                    var errorMessage = claimStatusInfo.FreeformMessageText_12;

                    var stcSegments = new (C043_HealthCareClaimStatus_2 Stc, string Position)[]
                    {
                        (claimStatusInfo.HealthCareClaimStatus_01, "STC01"),
                        (claimStatusInfo.HealthCareClaimStatus_10, "STC10"),
                        (claimStatusInfo.HealthCareClaimStatus_11, "STC11"),
                    };

                    foreach (var (stc, position) in stcSegments)
                    {
                        if (stc == null || string.IsNullOrEmpty(stc.HealthCareClaimStatusCategoryCode_01))
                            continue;

                        var claimStatusCodeEntity = claimStatusCodes.FirstOrDefault(x => x.Code == stc.StatusCode_02);
                        var claimStatusCategoryCodeEntity = claimStatusCategoryCodes.FirstOrDefault(x => x.Code == stc.HealthCareClaimStatusCategoryCode_01);

                        refValidationId = await SaveClaimStatusError(
                            ClaimErrorNumber.EraFunderRejected, submissionEntity,
                            claimStatusCodeEntity, claimStatusCategoryCodeEntity,
                            errorMessage, refValidationId, ediDownloadData,
                            stc.EntityIdentifierCode_03, position);

                        isCodePresent = true;
                    }
                }

                if (isCodePresent)
                {
                    await _claimService.UpdateClaimSubmissionStatus(submissionEntity, ClaimSubmissionStatus.FunderRejected);
                    await _claimService.UpdateClaimStatus(submissionEntity.ClaimId, submissionEntity.Claim.AccountInfoId, ClaimStatus.RejectedFunder);
                    await _claimService.AddResponseHistory(submissionEntity.Claim.Id, ClaimActionMode.System, ClaimAction.ClaimProcessing, ClaimHistoryAction.ClaimResponseRejected277);
                }

                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Processed, BlobFolderNames.Rejected);
            }
            else
            {
                _logger.LogError($"Submission entity with identifier: {submissionIdentifier} not found");
                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Errors, null);
            }
        }

        private async Task UploadIntoBillingBlob(BillingFolderStructureModel model, BlobFolderNames folderName, BlobFolderNames? subFolderName)
        {
            var dataString = System.Text.Encoding.UTF8.GetString(model.Data);
            var transactionControlResult = await _billingFilePath.GetTransactionControlNumber(dataString);
            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(transactionControlResult);

            var billingRequest = new BillingRequest
            {
                FieldIdentifier = model.FileName,
                FolderName = folderName.ToString(),
                AccountInfoId = model.AccountInfoId,
                Data = model.Data,
                BillingContainerName = BillingConstants.BillingContainerName,
                TransactionNumber = result?.Id,
                SubFolderName = subFolderName.ToString(),
                ClearingHouseTitle = Enum.GetName(typeof(BillingClearingHousesEnum), model.ClearingHouseId),
                ClearingHouseId = model.ClearingHouseId
            };

            var billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            await _ediFilesDownload.SaveClaimEdiFilePath(billingRequest, fullFilePath, result);
            await _billingBlobService.UploadIntoContainerAsync(BillingConstants.BillingContainerName, fullFilePath, new MemoryStream(model.Data));
            billingRequest.FolderName = BlobFolderNames.Incoming.ToString();
            billingRequest.SubFolderName = null;
            billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            await _billingBlobService.DeleteBlobFromContainerAsync(BillingConstants.BillingContainerName, fullFilePath);
        }

        private async Task HandleAcceptStatus(Loop_2200D_277A claimStatusLoop, EdiDownloadData ediDownloadData, byte[] data, int? acccountInfoId, string fileIdentifier)
        {
            var claimStatusInformation = claimStatusLoop.STC_ClaimLevelStatusInformation.FirstOrDefault();
            var statusCategoryCode = claimStatusInformation.HealthCareClaimStatus_01.HealthCareClaimStatusCategoryCode_01.Split(":")[0];
            var claimStatus = claimStatusInformation.HealthCareClaimStatus_01;

            var fullStatusCode = !string.IsNullOrEmpty(claimStatus.HealthCareClaimStatusCategoryCode_01) &&
                                 claimStatus.HealthCareClaimStatusCategoryCode_01.Contains(":")
                                 ? claimStatus.HealthCareClaimStatusCategoryCode_01
                                 : claimStatus.HealthCareClaimStatusCategoryCode_01 +
                                   (string.IsNullOrEmpty(claimStatus.StatusCode_02) ? "" : ":" + claimStatus.StatusCode_02);

            var submissionIdentifier = claimStatusLoop.TRN_ClaimStatusTrackingNumber.CurrentTransactionTraceNumber_02;

            var submissionEntity = _claimSubmissionRepository.Query().Include(x => x.Claim)
                .Where(x => x.ClaimSubmissionIdentifier == submissionIdentifier).OrderByDescending(x => x.Id).FirstOrDefault();

            var billingBlobModel = new BillingFolderStructureModel
            {
                Data = data,
                FileName = fileIdentifier,
                AccountInfoId = acccountInfoId,
                ClearingHouseId = ediDownloadData.ClearingHouseId
            };

            if (submissionEntity != null)
            {
                if (statusCategoryCode == _claimStatusCategoryAcceptCode) // ignore A0 & A1 here and do nothing....
                {
                    await _claimService.UpdateClaimStatus(submissionEntity.ClaimId, submissionEntity.Claim.AccountInfoId, ClaimStatus.AcceptedFunder);
                    await _claimService.UpdateClaimSubmissionStatus(submissionEntity, ClaimSubmissionStatus.FunderAccepted);
                    await _claimService.AddResponseHistory(submissionEntity.Claim.Id, ClaimActionMode.System, ClaimAction.ClaimProcessing, ClaimHistoryAction.ClaimResponseAccepted277);

                    var clearingHouseDetails = new ClearingHouseDetailsSaveModel
                    {
                        submissionId = submissionEntity.Id,
                        claimId = submissionEntity.ClaimId,
                        memberId = 0,
                        fileTypeId = ClaimResponseFileType.File277,
                        batchId = submissionEntity.ClaimSubmissionIdentifier,
                        isAccepted = true,
                        validationErrorId = 0,
                        fileIdentifier = ediDownloadData.FileIdentifier,
                        downloadDateTime = ediDownloadData.DownloadDateTime,
                        clearingHouseId = ediDownloadData.ClearingHouseId
                    };
                    await _claimService.AddClearingHouseDetailsAsync(clearingHouseDetails);

                    await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Processed, BlobFolderNames.AcceptedWithErrors);
                }
                else if (_claimStatusCategoryIgnoreCodes.Contains(statusCategoryCode)) // show Received status for A0 & A1....
                {
                    var clearingHouseDetails = new ClearingHouseDetailsSaveModel();
                    var isA1Accepted = fullStatusCode.Equals("A1:20", StringComparison.OrdinalIgnoreCase);
                    if (isA1Accepted)
                    {
                        await _claimService.UpdateClaimStatus(submissionEntity.ClaimId, submissionEntity.Claim.AccountInfoId, ClaimStatus.AcceptedFunder);
                        await _claimService.UpdateClaimSubmissionStatus(submissionEntity, ClaimSubmissionStatus.FunderAccepted);
                        _logger.LogInformation($"Claim with submission identifier: {submissionIdentifier} is accepted by clearing house with status code: {fullStatusCode}. Updated claim status to AcceptedFunder and claim submission status to FunderAccepted");
                    }

                    await _claimService.AddResponseHistory(submissionEntity.Claim.Id, ClaimActionMode.System, ClaimAction.ClaimProcessing, ClaimHistoryAction.ClaimResponseReceived277);

                    clearingHouseDetails = new ClearingHouseDetailsSaveModel
                    {
                        submissionId = submissionEntity.Id,
                        claimId = submissionEntity.ClaimId,
                        memberId = 0,
                        fileTypeId = ClaimResponseFileType.File277,
                        batchId = submissionEntity.ClaimSubmissionIdentifier,
                        isAccepted = isA1Accepted,
                        validationErrorId = 0,
                        fileIdentifier = ediDownloadData.FileIdentifier,
                        downloadDateTime = ediDownloadData.DownloadDateTime,
                        clearingHouseId = ediDownloadData.ClearingHouseId
                    };
                    await _claimService.AddClearingHouseDetailsAsync(clearingHouseDetails);

                    await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Processed, BlobFolderNames.Accepted);
                }
            }
            else
            {
                _logger.LogError($"Submission entity with identifier: {submissionIdentifier} not found");

                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Errors, null);
            }
        }

        private async Task<int> SaveClaimStatusError(ClaimErrorNumber errorNumber, ClaimSubmissionEntity claimsubmission, ExternalCodeEntity claimStatusCodeEntity,
             ExternalCodeEntity claimStatusCategoryCodeEntity, string errorMessage, int refValidationId, EdiDownloadData ediDownloadData, string entityIdentifierCode = null, string stcPosition = "STC01")
        {
            if (claimStatusCodeEntity != null && claimStatusCategoryCodeEntity != null)
            {
                try
                {
                    var predefinedErrorMessage = _claimErrorMessageRepository.Query().FirstOrDefault(x => x.ErrorNumber == errorNumber);
                    var error = new ClaimValidationErrorEntity
                    {
                        ClaimId = claimsubmission.Claim.Id,
                        ClaimSubmissionId = claimsubmission.Id,
                        ClaimErrorMessageId = predefinedErrorMessage.Id,
                        ContextMessage = errorMessage,
                        ValidationDate = EstDateTime,
                        ClaimErrorSource = ClaimErrorSource.Era,
                        RefValidationId = refValidationId,
                        EraValidationError = new EraValidationErrorEntity
                        {
                            AdjustmentLevel = AdjustmentLevel.Claim,
                            GroupCodeId = claimStatusCodeEntity.Id,
                            AdjustmentCodeId = claimStatusCategoryCodeEntity.Id,
                            EntityIdentifierCode = entityIdentifierCode,
                            StcPosition = stcPosition
                        },
                    };

                    MarkCreated(error, 0);
                    _claimValidationErrorRepository.Add(error);
                    await _claimValidationErrorRepository.CommitAsync();

                    var clearingHouseDetails = new ClearingHouseDetailsSaveModel
                    {
                        submissionId = claimsubmission.Id,
                        claimId = claimsubmission.ClaimId,
                        validationErrorId = error.Id,
                        memberId = 0,
                        fileTypeId = ClaimResponseFileType.File277,
                        batchId = claimsubmission.ClaimSubmissionIdentifier,
                        isAccepted = false,
                        fileIdentifier = ediDownloadData.FileIdentifier,
                        downloadDateTime = ediDownloadData.DownloadDateTime,
                        clearingHouseId = ediDownloadData.ClearingHouseId
                    };
                    await _claimService.AddClearingHouseDetailsAsync(clearingHouseDetails);

                    return refValidationId > 0 ? refValidationId : error.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error saving claim error: " +
                                         $"for claim id={claimsubmission.Claim.Id}, memberId={0}" +
                                         $"Error: {ex.Message}");
                    throw;
                }

            }
            return 0;
        }
    }
}
