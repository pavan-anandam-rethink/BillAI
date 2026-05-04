using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Files;
using EdiFabric.Core.Model.Edi;
using EdiFabric.Templates.Hipaa5010_999;
using EraParserService.Domain.Enums.Edi999;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
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

namespace EraParserService.Domain.Services.EdiParsers.Edi999
{
    public class Edi999Parser : BaseService, IEdi999Parser
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IRepository<BillingDbContext, ClaimValidationErrorEntity> _claimValidationErrorRepository;
        private readonly IRepository<BillingDbContext, ClaimErrorMessageEntity> _claimErrorMessageRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _claimSubmissionRepository;
        private readonly IRepository<BillingDbContext, ExternalCodeEntity> _externalCodeRepository;
        private readonly IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity> _clearingHouseResponseRepository;
        private readonly IBaseClaimService _claimService;
        private readonly IBillingBlobService _billingBlobService;
        private readonly BillingDbContext _billingDbContext;
        private readonly IBillingFilePath _billingFilePath;
        private readonly IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> _claimSubmissionFunderSequence;
        private readonly IEdiFilesDownload _ediFilesDownload;

        public Edi999Parser(
            ILoggerFactory loggerFactory,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, ClaimSubmissionEntity> claimSubmissionRepository,
            IRepository<BillingDbContext, ClaimErrorMessageEntity> claimErrorMessageRepository,
            IRepository<BillingDbContext, ClaimValidationErrorEntity> claimValidationErrorRepository,
            IRepository<BillingDbContext, ExternalCodeEntity> externalCodeRepository,
            IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity> clearingHouseResponseRepository,
            IBaseClaimService claimService, IBillingBlobService billingBlobService, BillingDbContext billingDbContext,
            IBillingFilePath billingFilePath, IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> claimSubmissionFunderSequence,
            IEdiFilesDownload ediFilesDownload)
        {
            _claimValidationErrorRepository = claimValidationErrorRepository;
            _claimErrorMessageRepository = claimErrorMessageRepository;
            _claimRepository = claimRepository;
            _claimSubmissionRepository = claimSubmissionRepository;
            _externalCodeRepository = externalCodeRepository;
            _clearingHouseResponseRepository = clearingHouseResponseRepository;
            _claimService = claimService;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(GetType());
            _billingBlobService = billingBlobService;
            _billingDbContext = billingDbContext;
            _billingFilePath = billingFilePath;
            _claimSubmissionFunderSequence = claimSubmissionFunderSequence;
            _ediFilesDownload = ediFilesDownload;
        }

        public async Task ParseAsync(EdiDownloadData ediDownloadData, List<IEdiItem> ediItems, int? transactionNumber, string fileIdentifier, byte[] data)
        {

            var claimEntity = _billingDbContext.ClaimSubmissions.AsQueryable()
                    .Include(x => x.Claim)
                    .FirstOrDefault(cs => cs.Id == transactionNumber);

            _logger.LogInformation($"[{DateTime.Now:G}]: Parsing 999 file: {ediDownloadData.FileIdentifier} for Account {ediDownloadData.AccountInfoId}");

            var hc999List = ediItems.OfType<TS999>().ToList();
            foreach (var hc999Item in hc999List)
            {
                // Functional Group Response Segment
                var fgrTrailer = hc999Item.AK9;
                var primaryStatus = GetFunctionalGroupAcknowledgmentCodeFromString(fgrTrailer.FunctionalGroupAcknowledgeCode_01);

                // Information in this loop is required when an error is present in the claim submitted
                var transactionSetResponseLoop = hc999Item.Loop_2000;
                foreach (var transactionSet in transactionSetResponseLoop)
                {
                    // Transaction Set Response Segment
                    var tsrHeader = transactionSet.AK2;
                    var tsrTrailer = transactionSet.IK5;
                    var errorIdentificationLoop = transactionSet.Loop_2100;
                    var tsrStatusCode = GetTransactionSetAcknowledgmentCodeFromString(tsrTrailer.TransactionSetAcknowledgmentCode_01);
                    var controlNumber = int.Parse(tsrHeader.TransactionSetControlNumber_02);

                    var submissionEntity = _claimSubmissionRepository.Query().Include(x => x.Claim).FirstOrDefault(x => x.Id == controlNumber);
                    if (submissionEntity != null)
                    {
                        await HandleTransactionSetResponseStatus(tsrStatusCode, submissionEntity, errorIdentificationLoop, ediDownloadData, transactionNumber, fileIdentifier, data);
                    }
                    else
                    {
                        _logger.LogError($"Submission entity with id: {controlNumber} not found");
                    }
                }
            }
        }

        private async Task HandleTransactionSetResponseStatus(TransactionSetAcknowledgmentCode code, ClaimSubmissionEntity claimSubmission, List<Loop_2100> errorLoop, EdiDownloadData ediDownloadData, int? transactionNumber, string fileIdentifier, byte[] data)
        {
            // Fetch both 277 and 999 responses in a single DB call
            var alreadySavedResponses = await _clearingHouseResponseRepository.Query()
                                                                             .AsNoTracking()
                                                                             .Where(x => x.BatchId == claimSubmission.ClaimSubmissionIdentifier &&
                                                                                         (x.ResponseFileTypeId == ClaimResponseFileType.File277 ||
                                                                                          x.ResponseFileTypeId == ClaimResponseFileType.File999) &&
                                                                                         x.ClearingHouseId == ediDownloadData.ClearingHouseId &&
                                                                                         x.DateDeleted == null)
                                                                             .ToListAsync();

            var alreadySaved277Response = alreadySavedResponses.FirstOrDefault(x => x.ResponseFileTypeId == ClaimResponseFileType.File277);
            var alreadySaved999Response = alreadySavedResponses.FirstOrDefault(x => x.ResponseFileTypeId == ClaimResponseFileType.File999);

            // Check if a 277 has already set a funder-level status on this submission.
            // A 277 record may exist for A0/A1 "received" acknowledgments that do NOT update claim status,
            // so we must check the actual current submission status to determine if a funder-level decision was made.
            var isFunderLevelStatusAlreadySet = alreadySaved277Response != null &&
                (claimSubmission.SubmissionStatus == ClaimSubmissionStatus.FunderAccepted ||
                 claimSubmission.SubmissionStatus == ClaimSubmissionStatus.FunderRejected ||
                 claimSubmission.SubmissionStatus == ClaimSubmissionStatus.FunderReceived ||
                 claimSubmission.SubmissionStatus == ClaimSubmissionStatus.FunderSubmitted ||
                 claimSubmission.SubmissionStatus == ClaimSubmissionStatus.FunderPending ||
                 claimSubmission.SubmissionStatus == ClaimSubmissionStatus.FunderDenied ||
                 claimSubmission.SubmissionStatus == ClaimSubmissionStatus.FunderProcessed);

            var billingFilePath = string.Empty;
            var parts = fileIdentifier.Split('/');
            string fileName = parts[^1];
            string partner = parts[2];
            var summary = EDI999Reader.Parse(ediDownloadData.EdiData, fileName, partner);
            var billingRequest = new BillingRequest
            {
                FieldIdentifier = fileName,
                FolderName = BlobFolderNames.Reports.ToString(),
                AccountInfoId = claimSubmission.Claim.AccountInfoId,
                Data = data,
                BillingContainerName = BillingConstants.BillingContainerName,
                TransactionNumber = transactionNumber,
                SubFolderName = BlobFolderNames.Daily.ToString(),
                ClearingHouseTitle = Enum.GetName(typeof(BillingClearingHousesEnum), ediDownloadData.ClearingHouseId),
                ClearingHouseId = ediDownloadData.ClearingHouseId
            };
            billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            await _billingBlobService.Update999ReportAsync(BillingConstants.BillingContainerName, summary, fullFilePath);

            var billingBlobModel = new BillingFolderStructureModel
            {
                Data = data,
                FileName = fileIdentifier,
                AccountInfoId = billingRequest.AccountInfoId,
                ClearingHouseId = ediDownloadData.ClearingHouseId,
            };
            switch (code)
            {
                case TransactionSetAcknowledgmentCode.Accepted:
                    await _claimService.AddResponseHistory(claimSubmission.Claim.Id, ClaimActionMode.System, ClaimAction.ClaimProcessing, ClaimHistoryAction.ClaimResponseAccepted999);
                    var clearingHouseDetails = new ClearingHouseDetailsSaveModel
                    {
                        submissionId = claimSubmission.Id,
                        claimId = claimSubmission.ClaimId,
                        memberId = 0,
                        fileTypeId = ClaimResponseFileType.File999,
                        batchId = claimSubmission.ClaimSubmissionIdentifier,
                        isAccepted = true,
                        validationErrorId = 0,
                        fileIdentifier = fileName,
                        downloadDateTime = ediDownloadData.DownloadDateTime,
                    };
                    await _claimService.AddClearingHouseDetailsAsync(clearingHouseDetails);

                    // Only update claim/submission status if 277 has NOT already set a funder-level status.
                    // A 277 with A0/A1 (non-A1:20) saves a response record but does NOT update claim status,
                    // so the 999 must still update in that case.
                    if (!isFunderLevelStatusAlreadySet)
                    {
                        await _claimService.UpdateClaimStatus(claimSubmission.ClaimId, claimSubmission.Claim.AccountInfoId, ClaimStatus.AcceptedClearingHouse);
                        await _claimService.UpdateClaimSubmissionStatus(claimSubmission, ClaimSubmissionStatus.ClearinghouseAccepted);
                    }
                    else
                    {
                        _logger.LogInformation($"277 already set funder-level status for submission {claimSubmission.Id}. Skipping 999 claim status update to avoid overwriting funder-level status.");
                    }

                    await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Processed, BlobFolderNames.Accepted);

                    break;

                case TransactionSetAcknowledgmentCode.AcceptedWithErrors:
                    await SaveClaimErrors(claimSubmission.ClaimId, claimSubmission.Id, ClaimErrorNumber.EraFunderAcceptedWithErrors, errorLoop, claimSubmission.ClaimSubmissionIdentifier, ediDownloadData);
                    await _claimService.AddResponseHistory(claimSubmission.Claim.Id, ClaimActionMode.System, ClaimAction.ClaimProcessing, ClaimHistoryAction.ClaimResponseAccepted999);

                    if (!isFunderLevelStatusAlreadySet)
                    {
                        await _claimService.UpdateClaimStatus(claimSubmission.ClaimId, claimSubmission.Claim.AccountInfoId, ClaimStatus.Pending);
                        await _claimService.UpdateClaimSubmissionStatus(claimSubmission, ClaimSubmissionStatus.ClearinghouseAccepted);
                    }
                    else
                    {
                        _logger.LogInformation($"277 already set funder-level status for submission {claimSubmission.Id}. Skipping 999 claim status update to avoid overwriting funder-level status.");
                    }

                    await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Processed, BlobFolderNames.AcceptedWithErrors);

                    break;

                case TransactionSetAcknowledgmentCode.Rejected:
                    await SaveClaimErrors(claimSubmission.ClaimId, claimSubmission.Id, ClaimErrorNumber.EraFunderRejected, errorLoop, claimSubmission.ClaimSubmissionIdentifier, ediDownloadData);
                    await _claimService.AddResponseHistory(claimSubmission.Claim.Id, ClaimActionMode.System, ClaimAction.ClaimProcessing, ClaimHistoryAction.ClaimResponseRejected999);

                    if (!isFunderLevelStatusAlreadySet)
                    {
                        await _claimService.UpdateClaimSubmissionStatus(claimSubmission, ClaimSubmissionStatus.ClearinghouseRejected);
                        await _claimService.UpdateClaimStatus(claimSubmission.ClaimId, claimSubmission.Claim.AccountInfoId, ClaimStatus.RejectedClearinghouse);
                    }
                    else
                    {
                        _logger.LogInformation($"277 already set funder-level status for submission {claimSubmission.Id}. Skipping 999 claim status update to avoid overwriting funder-level status.");
                    }

                    await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Processed, BlobFolderNames.Rejected);

                    break;

                default:
                    throw new ArgumentException($"Unknown Acknowledgment code = {code}", nameof(code));
            }

            if (alreadySaved999Response != null)
            {
                _logger.LogInformation($"999 already processed for submission {claimSubmission.Id}.");
                await _claimService.SetHistoryActionDate(alreadySaved999Response);
            }

            if (alreadySaved277Response != null)
            {
                await _claimService.SetHistoryActionDate(alreadySaved277Response);
            }
        }

        private async Task UploadIntoBillingBlob(BillingFolderStructureModel model, BlobFolderNames folderName, BlobFolderNames subFolderName)
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

        private TransactionSetAcknowledgmentCode GetTransactionSetAcknowledgmentCodeFromString(string code)
        {
            return (TransactionSetAcknowledgmentCode)code[0];
        }

        private FunctionalGroupAcknowledgmentCode GetFunctionalGroupAcknowledgmentCodeFromString(string code)
        {
            return (FunctionalGroupAcknowledgmentCode)code[0];
        }

        private async Task SaveClaimErrors(int claimId, int submissionId, ClaimErrorNumber errorNumber, List<Loop_2100> errorLoop, string batchId, EdiDownloadData ediDownloadData)
        {
            try
            {
                var claimRejectErrorMessage = _claimErrorMessageRepository.Query().FirstOrDefault(x => x.ErrorNumber == errorNumber);
                var dataElementErrorCodeList = await _externalCodeRepository.Query()
                    .Where(x => x.CodeTypeId == ExternalCodeType.ElementSyntaxErrorCode)
                    .ToListAsync();
                var dataSegmentErrorCodeList = await _externalCodeRepository.Query()
                    .Where(x => x.CodeTypeId == ExternalCodeType.SegmentSyntaxErrorCode)
                    .ToListAsync();

                foreach (var error in errorLoop)
                {
                    var errorSegment = error.IK3;
                    var errorSegmentCode = dataSegmentErrorCodeList.FirstOrDefault(x => x.Code == errorSegment.ImplementationSegmentSyntaxErrorCode_04);
                    var segmentErrorMessage = $"SegmentId: {errorSegment.SegmentIDCode_01}. Position in segment: {errorSegment.SegmentPositioninTransactionSet_02}.";
                    var elementErrorLoop = error.Loop_2110 ?? new List<Loop_2110>();

                    var claimValidationErrorEntity = new ClaimValidationErrorEntity
                    {
                        ClaimId = claimId,
                        ClaimSubmissionId = submissionId,
                        ClaimErrorMessageId = claimRejectErrorMessage.Id,
                        ClaimErrorSource = ClaimErrorSource.Era,
                        ContextMessage = segmentErrorMessage,
                        ValidationDate = EstDateTime,
                        RefValidationId = 0,
                        EraValidationError = new EraValidationErrorEntity
                        {
                            GroupCodeId = errorSegmentCode.Id,
                            AdjustmentLevel = AdjustmentLevel.Claim,
                        }
                    };
                    await AddValidationError(claimValidationErrorEntity);

                    var refValidationId = claimValidationErrorEntity.Id;

                    var clearingHouseDetails = new ClearingHouseDetailsSaveModel
                    {
                        submissionId = submissionId,
                        claimId = claimId,
                        memberId = 0,
                        fileTypeId = ClaimResponseFileType.File999,
                        batchId = batchId,
                        isAccepted = false,
                        validationErrorId = refValidationId,
                        fileIdentifier = ediDownloadData.FileIdentifier,
                        downloadDateTime = ediDownloadData.DownloadDateTime,
                        clearingHouseId = ediDownloadData.ClearingHouseId
                    };
                    await _claimService.AddClearingHouseDetailsAsync(clearingHouseDetails);

                    foreach (var element in elementErrorLoop)
                    {
                        var errorCode = dataElementErrorCodeList.FirstOrDefault(x => x.Code == element.IK4.ImplementationDataElementSyntaxErrorCode_03);
                        var elementError = element.IK4;
                        var elementErrorMessage =
                            $"{(elementError.CopyofBadDataElement_04 != null ? $"Value: {elementError.CopyofBadDataElement_04}. " : string.Empty)}" +
                            $"{(elementError.DataElementReferenceNumber_02 != null ? $"Reference number: {elementError.DataElementReferenceNumber_02}. " : string.Empty)}" +
                            $"Position in segment: {elementError.PositionInSegment_01.ElementPositionInSegment_01}.";

                        claimValidationErrorEntity = new ClaimValidationErrorEntity
                        {
                            ClaimId = claimId,
                            ClaimSubmissionId = submissionId,
                            ClaimErrorMessageId = claimRejectErrorMessage.Id,
                            ClaimErrorSource = ClaimErrorSource.Era,
                            ContextMessage = elementErrorMessage,
                            ValidationDate = EstDateTime,
                            RefValidationId = refValidationId,
                            EraValidationError = new EraValidationErrorEntity
                            {
                                GroupCodeId = errorSegmentCode.Id,
                                AdjustmentCodeId = errorCode.Id,
                                AdjustmentLevel = AdjustmentLevel.Claim,
                            }
                        };

                        await AddValidationError(claimValidationErrorEntity);

                        clearingHouseDetails = new ClearingHouseDetailsSaveModel
                        {
                            submissionId = submissionId,
                            claimId = claimId,
                            memberId = 0,
                            fileTypeId = ClaimResponseFileType.File999,
                            batchId = batchId,
                            isAccepted = false,
                            validationErrorId = claimValidationErrorEntity.Id,
                            fileIdentifier = ediDownloadData.FileIdentifier,
                            downloadDateTime = ediDownloadData.DownloadDateTime,
                            clearingHouseId = ediDownloadData.ClearingHouseId
                        };
                        await _claimService.AddClearingHouseDetailsAsync(clearingHouseDetails);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving claim error: " +
                     $"for claimId={claimId}, submissionId={submissionId}  memberId={0}" +
                     $"Error: {ex.Message}");
                throw;
            }
        }

        private async Task AddValidationError(ClaimValidationErrorEntity errorEntity)
        {
            MarkCreated(errorEntity, 0);
            await _claimValidationErrorRepository.AddAsync(errorEntity);
            await _claimValidationErrorRepository.CommitAsync();
        }


    }
}
