using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Files;
using BillingService.Domain.Models.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Linq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Extensions;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class CHService : BaseService, ICHService
    {

        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IMessageBus _bus;
        private readonly IFileManagerService _fileManager;
        private readonly IFileService _fileService;
        private readonly IBlobProcessingService _blobProcessingService;
        private readonly IBillingBlobService _billingBlobService;
        private readonly IBillingFilePath _billingFilePath;
        private readonly IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> _claimSubmissionFunderSequence;
        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _claimSubmissionEntity;
        private readonly ILogger<CHService> _logger;
        private readonly IEdiFilesDownload _ediFilesDownload;

        public CHService(
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IMessageBus bus,
            IFileManagerService fileManager, IFileService fileService,
            IBlobProcessingService blobProcessingService,
            IBillingBlobService billingBlobService,
            IBillingFilePath billingFilePath,
            IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> claimSubmissionFunderSequence,
            IRepository<BillingDbContext, ClaimSubmissionEntity> claimSubmissionEntity,
            ILogger<CHService> logger,
            IEdiFilesDownload ediFilesDownload)
        {
            _claimRepository = claimRepository;
            _bus = bus;
            _fileManager = fileManager;
            _fileService = fileService;
            _blobProcessingService = blobProcessingService;
            _billingBlobService = billingBlobService;
            _billingFilePath = billingFilePath;
            _claimSubmissionFunderSequence = claimSubmissionFunderSequence;
            _claimSubmissionEntity = claimSubmissionEntity;
            _logger = logger;
            _ediFilesDownload = ediFilesDownload;
        }

        public async Task<bool> UploadFileAsync(ClaimUploadModelWithUserInfo model)
        {
            try
            {
                var claim = await _claimRepository.Query().FirstOrDefaultAsync(e =>
                e.Id == model.ClaimId && e.DateDeleted == null);

                if (claim == null)
                    throw new ArgumentException(nameof(model.ClaimId));

                if (model.ClaimId == null)
                    throw new ArgumentException(nameof(model.ClaimId));

                if (model.Data == null || model.Data.Length <= 0)
                    throw new ArgumentException();

                var fileName = Path.GetFileNameWithoutExtension(model.FileName);
                var fileExtension = Path.GetExtension(model.FileName);

                var filePath = _fileService.PrepareFolderForEncounterAttachmentFile(claim.AccountInfoId,
                    fileName + fileExtension, "EDIData", true, null, claim.ClaimIdentifier);

                //await _fileManager.UploadFileAsync(filePath, model.FileName, new MemoryStream(model.Data));

                //var fullPath = filePath + model.FileName;
                //var fullPathLink = await _fileManager.GetFileUrl(fullPath, 30);

                var billingBlobModel = new BillingFolderStructureModel
                {
                    Data = model.Data,
                    FileName = model.FileName,
                    ClearingHouseId = model.ClearingHouseId
                };
                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Processing, null);

                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        // Update usages of GetTransactionControlNumber to handle the new return type (Task<(int? ControlNumber, string? ClaimIdentifier)>)

        // In UploadIntoBillingBlob
        private async Task UploadIntoBillingBlob(BillingFolderStructureModel model, BlobFolderNames folderName, BlobFolderNames? subFolderName)
        {
            var dataString = System.Text.Encoding.UTF8.GetString(model.Data);
            var transactionResult = await _billingFilePath.GetTransactionControlNumber(dataString);

            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(transactionResult);
            int accountInfoId = result != null
                    ? result.Claim.AccountInfoId
                    : 0;

            var billingRequest = new BillingRequest
            {
                FieldIdentifier = model.FileName,
                FolderName = folderName.ToString(),
                AccountInfoId = accountInfoId,
                Data = model.Data,
                BillingContainerName = BillingConstants.BillingContainerName,
                TransactionNumber = result?.Id,
                SubFolderName = subFolderName.ToString(),
                ClearingHouseTitle = Enum.GetName(typeof(BillingClearingHousesEnum), model.ClearingHouseId ?? 0)
            };

            var billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            var modelData = model.Data;
            if (subFolderName != null) model.Data = Encoding.UTF8.GetBytes(model.Message);
            await _billingBlobService.UploadIntoContainerAsync(BillingConstants.BillingContainerName, fullFilePath, new MemoryStream(model.Data));
            model.Data = modelData;
            if (transactionResult.FileType == FileTypes.Type835.ToString())
            {
                billingRequest.FolderName = BlobFolderNames.Incoming.ToString();
                billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
                (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
                await (string.IsNullOrEmpty(billingRequest.SubFolderName) ? _billingBlobService.DeleteBlobFromContainerAsync(BillingConstants.BillingContainerName, fullFilePath) : Task.CompletedTask);
            }
            else
            {
                billingRequest.FolderName = BlobFolderNames.Outgoing.ToString();
                billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
                (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
                await _billingBlobService.DeleteBlobFromContainerAsync(BillingConstants.BillingContainerName, fullFilePath);
            }
        }

        public async Task<bool> UploadERAErrorFileAsync(ERAUploadModel model)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(model.fileName);
                var fileExtension = Path.GetExtension(model.fileName);

                var filePath = _fileService.PrepareFolderForERAErrorFile(model.accountInfoId);

                var billingBlobModel = new BillingFolderStructureModel
                {
                    Data = model.data,
                    FileName = model.fileName
                };
                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Failed, null);
                billingBlobModel.Message = $"[ERROR] AccountInfoId not found for ClaimIdentifier '{model.claimIdentifier}' in DB.\nFile: {fileName}\nTimestamp: {DateTime.UtcNow:G}";
                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Logs, BlobFolderNames.ErrorLogs);

                await _fileManager.UploadFileAsync(filePath, model.fileName, new MemoryStream(model.data), model.containerName);

                var fullPath = filePath + model.fileName;
                var fullPathLink = await _fileManager.GetFileUrl(fullPath, 30, model.containerName);

                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        // In UploadEDIResponseFile
        public async Task<bool> UploadEDIResponseFile(DownloadSftpDataModel fileStreams)
        {
            try
            {
                var containerName = fileStreams.Title.ToLowerInvariant();
                var memoryStream = new MemoryStream(fileStreams.Data);

                var billingFilePath = string.Empty;
                var dataString = System.Text.Encoding.UTF8.GetString(fileStreams.Data);
                var transactionResult = await _billingFilePath.GetTransactionControlNumber(dataString);

                var result = await _billingFilePath.FetchClaimSubmissionDataForERA(transactionResult);
                int accountInfoId = result != null
                        ? result.Claim.AccountInfoId
                        : _claimSubmissionEntity.Query().AsNoTracking()
                       .Where(x => x.AccountFederalTaxId == transactionResult.FederalTaxId || x.AccountNpiNumber == transactionResult.NpiNumber)
                       .Select(x => x.Claim.AccountInfoId)
                       .FirstOrDefault();

                var claimNumber = result != null
                    ? $"{result?.Id}_{EstDateTime:yyMM}.edi"
                    : $"{(fileStreams.FileName ?? transactionResult.NpiNumber)}_{EstDateTime:yyMM}.edi";

                var billingRequest = new BillingRequest
                {
                    FieldIdentifier = claimNumber,
                    FolderName = accountInfoId > 0 ? BlobFolderNames.Incoming.ToString() : BlobFolderNames.Failed.ToString(),
                    AccountInfoId = accountInfoId,
                    Data = fileStreams.Data,
                    BillingContainerName = BillingConstants.BillingContainerName,
                    TransactionNumber = result?.Id,
                    SubFolderName = null,
                    ClearingHouseTitle = Enum.GetName(typeof(BillingClearingHousesEnum), fileStreams.clearingHouseId),
                    ClearingHouseId = fileStreams.clearingHouseId
                };

                billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
                var (azureContainerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
                await _ediFilesDownload.SaveClaimEdiFilePath(billingRequest, fullFilePath, result);
                await _blobProcessingService.UploadIntoContainerAsync(containerName, fileStreams.FileName, memoryStream);
                await _billingBlobService.UploadIntoContainerAsync(BillingConstants.BillingContainerName, fullFilePath, new MemoryStream(fileStreams.Data));

                if (billingRequest.FolderName == BlobFolderNames.Failed.ToString())
                {
                    billingRequest.FolderName = BlobFolderNames.Logs.ToString();
                    billingRequest.SubFolderName = BlobFolderNames.ErrorLogs.ToString();
                    billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
                    (azureContainerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
                    fileStreams.Data = Encoding.UTF8.GetBytes($"Data is incomplete for file {claimNumber}");
                    await _billingBlobService.UploadIntoContainerAsync(BillingConstants.BillingContainerName, fullFilePath, new MemoryStream(fileStreams.Data));

                    _logger.LogError($"File uploading in the Failed Logs folder on the blob storage : FileName: {billingFilePath}");
                    return false;
                }

                var ediDownloadData = new EdiDownloadData
                {
                    ContainerName = containerName,
                    FileIdentifier = billingFilePath ?? fullFilePath,
                    DownloadDateTime = DateTimeExt.GetEasternDateTime(),
                    ClearingHouseId = fileStreams.clearingHouseId,
                    AccountInfoId = accountInfoId,
                };

                _logger.LogInformation($"File successfully uploaded on blob storage for filePath: {billingFilePath}");
                await _bus.SendAsync(ediDownloadData, Queues.RT_Billing_ClearingHouse_SFTPFiles_Download);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error at uploading EDI response file to Azure blob storage: FileName: {fileStreams.FileName}");
                return false;
            }
        }

        public async Task<bool> UploadEDIResponseFilesToBlobBackup(UploadAvailityFilesModel fileStreams)
        {
            try
            {
                if (fileStreams.files.Count > 0)
                {
                    foreach (var (fileStream, fileName) in fileStreams.files)
                    {
                        await _billingBlobService.UploadAvailityFilesToBlobBackupAsync(BillingConstants.AvailityContainerName, fileName, fileStream);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                var fileNames = string.Join(", ", fileStreams.files.Select(f => f.Item2));
                var errorMessage =
                    $"Error at uploading EDI response file to Billing Availity container: FileName(s): {fileNames}";

                _logger.LogError(ex, errorMessage);
                return false; // Ensure the method returns a Task<bool> in case of an exception.
            }

            return false;
        }
    }
}
