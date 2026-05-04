using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Files;
using BillingService.Domain.Models.Claims;
using ClearingHouseService.Web.Interface;
using ClearingHouseService.Web.Service;
using EraParserService.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Models.Claim;
using System.Text;
using Thon.Hotels.FishBus;

namespace ClearingHouseService.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClearingHouseProcessorController : ControllerBase
    {
        private readonly IClearingHouseProcessor _clearingHouseProcessor;
        private readonly IEdiUploadService _ediUploadService;
        private readonly ILogger<ClearingHouseProcessorController> _logger;
        private readonly IEdiDownloadService _ediDownloadService;
        private readonly IEdiProcessingService _eraProcessingService;
        private readonly ICommon _commonService;
        private readonly IBillingBlobService _billingBlobService;
        private readonly IBillingFilePath _billingFilePath;
        private readonly ICHService _blobBackupService;
        private readonly IConfiguration _configuration;
        private readonly IEdiFilesDownload _ediFilesDownload;

        public ClearingHouseProcessorController(IClearingHouseProcessor clearingHouseProcessor, IEdiUploadService ediUploadService,
            ILogger<ClearingHouseProcessorController> logger, IEdiDownloadService ediDownloadService, IEdiProcessingService eraProcessingService, ICommon commonService,
            IBillingBlobService billingBlobService, IBillingFilePath billingFilePath, ICHService blobBackupService, IConfiguration configuration, IEdiFilesDownload ediFilesDownload)
        {
            _clearingHouseProcessor = clearingHouseProcessor;
            _ediUploadService = ediUploadService;
            _logger = logger;
            _ediDownloadService = ediDownloadService;
            _eraProcessingService = eraProcessingService;
            _commonService = commonService;
            _billingBlobService = billingBlobService;
            _billingFilePath = billingFilePath;
            _blobBackupService = blobBackupService;
            _configuration = configuration;
            _ediFilesDownload = ediFilesDownload;
        }

        [HttpPost("uploadEdiDataToSftp")]
        public async Task<IActionResult> uploadEdiDataToSftp(ClearingHouseClaimModel claimModelDto)
        {
            _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: Starting processing claim with Id: {claimModelDto.claimId},clearinghouseId: {claimModelDto.clearinghouseId}");
            try
            {
                var (ediSuccess, ediResult) = await _clearingHouseProcessor.GenerateEDI(claimModelDto);
                if (ediSuccess && !string.IsNullOrEmpty(ediResult))
                {
                    byte[] Data = Encoding.UTF8.GetBytes(ediResult);

                    _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: Starting processing claim with Id: {claimModelDto.claimId}");

                    var uploadResult = await _ediUploadService.ProcessClaimAsync(claimModelDto.claimId, Convert.ToString(ediResult), claimModelDto.clearinghouseId);

                    var billingBlobModel = new BillingFolderStructureModel
                    {
                        Data = Data,
                        FileName = uploadResult.FileName,
                        Message = uploadResult.ErrorMessage ?? ($"File {uploadResult.FileName} Succesfully processed."),
                        ClearingHouseId = claimModelDto.clearinghouseId
                    };

                    if (uploadResult != null && uploadResult.IsSuccess)
                    {
                        var inputdata = new ClaimUploadModelWithUserInfo
                        {
                            ClaimId = claimModelDto.claimId,
                            Data = Data,
                            FileName = uploadResult.FileName,
                            ClearingHouseId = claimModelDto.clearinghouseId
                        };

                        var (uploadSuccess, uploadRes) = await _clearingHouseProcessor.UploadfileToBlobStorage(inputdata);

                        await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Logs, BlobFolderNames.ProcessingLogs);

                        if (uploadSuccess)
                        {
                            await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Archive, null);

                            if (claimModelDto.isSecondary)
                            {
                                if (!await _commonService.ReapplyPRAdjustmentAfterSecondaryBilling(claimModelDto.claimId))
                                {
                                    _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: EDI file uploaded to Azure blob storage for Claim Id : {claimModelDto.claimId}, Patient Responsibility Amount Reversal failed");
                                    return Ok();
                                }
                            }
                            _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: Ended processing claim with Id: {claimModelDto.claimId},clearinghouseId: {claimModelDto.clearinghouseId}");
                            return Ok();
                        }
                        else
                        {
                            _logger.LogError($"{nameof(ClearingHouseProcessorController)}: EDI file upload to Azure blob storage failed for Claim Id : {claimModelDto.claimId} - {uploadRes}");
                            return BadRequest($"Failed to upload EDI file to Azure blob storage : + {claimModelDto.claimId}{uploadRes}");
                        }
                    }
                    else
                    {
                        await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Failed, null);
                        await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Logs, BlobFolderNames.ErrorLogs);

                        _logger.LogError($"{nameof(ClearingHouseProcessorController)}: EDI upload to SFTP folder failed for Claim Id : {claimModelDto.claimId}");
                        return BadRequest($"Failed to upload EDI file to SFTP folder for Claim Id: + {claimModelDto.claimId}");
                    }
                }
                else
                {
                    _logger.LogError($"{nameof(ClearingHouseProcessorController)}: Failed to generate EDI for Claim Id : {claimModelDto.claimId}");
                    return BadRequest($"Failed to generate EDI for Claim Id : {claimModelDto.claimId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(ClearingHouseProcessorController)}: Error processing claim");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task UploadIntoBillingBlob(BillingFolderStructureModel model, BlobFolderNames folderName, BlobFolderNames? subFolderName)
        {
            try
            {
                _logger.LogInformation("{Controller}: Starting UploadIntoBillingBlob. AccountInfoId: {AccountInfoId}, Folder: {Folder}, SubFolder: {SubFolder}, FileName: {FileName}",
                    nameof(ClearingHouseProcessorController), model.AccountInfoId, folderName, subFolderName, model.FileName);

                var dataString = Encoding.UTF8.GetString(model.Data);

                _logger.LogInformation("Extracting Transaction Control Number from file. FileName: {FileName}", model.FileName);

                var transactionControlResult = await _billingFilePath.GetTransactionControlNumber(dataString);

                _logger.LogInformation("{Controller}: Fetching claim submission data for ERA. TransactionControlNumber: {TransactionControlNumber}",
                    nameof(ClearingHouseProcessorController), transactionControlResult.ControlNumbers);

                var result = await _billingFilePath.FetchClaimSubmissionDataForERA(transactionControlResult);

                int accountInfoId = result?.Claim?.AccountInfoId ?? 0;

                var claimNumber = result != null
                            ? $"{result.Id}_{DateTime.UtcNow:yyMM}.edi"
                            : string.Empty;

                var billingRequest = new BillingRequest
                {
                    FieldIdentifier = model.FileName ?? $"{claimNumber}_{DateTime.UtcNow:yyMM}.edi",
                    FolderName = folderName.ToString(),
                    AccountInfoId = accountInfoId,
                    Data = model.Data,
                    BillingContainerName = BillingConstants.BillingContainerName,
                    TransactionNumber = result?.Id,
                    SubFolderName = subFolderName?.ToString(),
                    ClearingHouseTitle = Enum.GetName(typeof(BillingClearingHousesEnum), model.ClearingHouseId ?? 0),
                    ClearingHouseId = model.ClearingHouseId
                };

                _logger.LogInformation("{Controller}: Creating billing folder path. AccountInfoId: {AccountInfoId}, TransactionId: {TransactionId}", nameof(ClearingHouseProcessorController),
                    billingRequest.AccountInfoId,
                    billingRequest.TransactionNumber);

                var billingFilePath =
                    await _billingFilePath.CreateFolderPath(billingRequest);

                var (containerName, fullFilePath) =
                    await _billingFilePath.SplitFilePath(billingFilePath);

                var originalData = model.Data;

                if (subFolderName != null)
                {
                    _logger.LogInformation("{Controller}: SubFolder detected. Uploading message file instead of original data. SubFolder: {SubFolder}", nameof(ClearingHouseProcessorController),
                        subFolderName);

                    model.Data = Encoding.UTF8.GetBytes(model.Message ?? string.Empty);
                }
                else
                {
                    var ediFilePathSaved = await _ediFilesDownload.SaveClaimEdiFilePath(billingRequest, fullFilePath, result);
                }

                _logger.LogInformation("{Controller}: Uploading file to Blob Storage. Container: {Container}, Path: {Path}", nameof(ClearingHouseProcessorController),
                    BillingConstants.BillingContainerName,
                    fullFilePath);

                await _billingBlobService.UploadIntoContainerAsync(BillingConstants.BillingContainerName, fullFilePath, new MemoryStream(model.Data));

                model.Data = originalData;

                if (string.IsNullOrWhiteSpace(billingRequest.SubFolderName))
                {
                    _logger.LogInformation("{Controller}: No SubFolder specified. Moving file from Processing folder. TransactionId: {TransactionId}", nameof(ClearingHouseProcessorController),
                        billingRequest.TransactionNumber);

                    billingRequest.FolderName = BlobFolderNames.Processing.ToString();

                    billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);

                    (_, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);

                    await _billingBlobService.DeleteBlobFromContainerAsync(BillingConstants.BillingContainerName, fullFilePath);
                }

                _logger.LogInformation("{Controller}: UploadIntoBillingBlob completed successfully. AccountInfoId: {billingRequest.AccountInfoId}, TransactionId: {billingRequest.TransactionNumber}",
                    nameof(ClearingHouseProcessorController),
                    billingRequest.AccountInfoId,
                    billingRequest.TransactionNumber);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Controller}: Error occurred while uploading file to Billing Blob. AccountInfoId: {AccountInfoId}, Folder: {Folder}, SubFolder: {SubFolder}, FileName: {FileName}",
                    nameof(ClearingHouseProcessorController), model.AccountInfoId, folderName, subFolderName, model.FileName);
                throw;
            }
        }

        [HttpPost("downloadEdiDataFromSftp")]
        public async Task<IActionResult> downloadEdiDataFromSftp([FromBody] int clearinghouseId)
        {
            try
            {
                if (clearinghouseId == 0)
                {
                    _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: Invalid clearing house Id : {clearinghouseId}");
                    return BadRequest($"Invalid clearing house ID.{clearinghouseId}");
                }
                var  clearingHouse = await _commonService.GetclearinghouseNameById(clearinghouseId);
                               
                _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: Starting EdiDownload for {clearingHouse.Title}");
                var fileStreams = await _ediDownloadService.downloadEdiDataFromSftp(clearingHouse);

                bool isAvailityFileUploaded = false;
                if (fileStreams != null && fileStreams.Count > 0)
                {
                    var uploadAvailityFilesModel = new UploadAvailityFilesModel { files = fileStreams, FilePath = _configuration["AvailityBackup"] };
                    isAvailityFileUploaded = await _blobBackupService.UploadEDIResponseFilesToBlobBackup(uploadAvailityFilesModel);
                }

                if (fileStreams != null && fileStreams.Count != 0)
                {
                    foreach (var (memoryStream, filename) in fileStreams)
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        var downloadedFiles = new DownloadSftpDataModel
                        {
                            Data = memoryStream.ToArray(),
                            FileName = filename,
                            Title = clearingHouse.Title,
                            clearingHouseId = clearinghouseId
                        };
                        var (uploadFileSuccess, uploadRes) = await _clearingHouseProcessor.UploadSFTPfilesToBlobStorage(downloadedFiles);
                        if (uploadFileSuccess && isAvailityFileUploaded)
                        {
                            bool isDeleted = await _ediDownloadService.DeleteFileFromSftp(clearingHouse, filename);
                            if (!isDeleted)
                            {
                                _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: Failed to delete file {filename} from SFTP server");
                            }
                        }
                    }
                    _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: Download finished for {clearingHouse.Title} (downloaded {fileStreams.Count} files)");
                    return Ok();
                }
                else
                {
                    _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: No files found to download for {clearingHouse.Title}");
                    return NoContent();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(ClearingHouseProcessorController)}: Error processing claim");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("processclearinghouseresponse")]
        public async Task<HandlerResult> ProcessClearingHouseResponse(EdiDownloadData downloadData)
        {
            _logger.LogInformation(
                $"{nameof(ClearingHouseProcessorController)}: Starting ClearingHouse response processing. FileIdentifier: {downloadData.FileIdentifier}, EdiData: {downloadData.EdiData}",
                downloadData.FileIdentifier,
                !string.IsNullOrWhiteSpace(downloadData.EdiData));

            try
            {
                HandlerResult result;

                if (!string.IsNullOrWhiteSpace(downloadData.EdiData))
                {
                    _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: EDI data detected. Preparing MemoryStream for FileIdentifier: {downloadData.FileIdentifier}",
                        downloadData.FileIdentifier);

                    await using var ediStream = new MemoryStream();
                    await using var streamWriter = new StreamWriter(ediStream);

                    await streamWriter.WriteAsync(downloadData.EdiData);
                    await streamWriter.FlushAsync();
                    ediStream.Position = 0;

                    _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: Invoking ERA processing service with EDI stream. FileIdentifier: {downloadData.FileIdentifier}",
                        downloadData.FileIdentifier);

                    result = await _eraProcessingService.ProcessFile(downloadData, ediStream);
                }
                else
                {
                    _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: No EDI data found. Processing without stream. FileIdentifier: {downloadData.FileIdentifier}",
                        downloadData.FileIdentifier);

                    result = await _eraProcessingService.ProcessFile(downloadData);
                }

                _logger.LogInformation($"{nameof(ClearingHouseProcessorController)}: Completed ClearingHouse response processing. FileIdentifier: {downloadData.FileIdentifier}",
                    downloadData.FileIdentifier);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(ClearingHouseProcessorController)}: Error processing ClearingHouse file. FileIdentifier: {downloadData.FileIdentifier}, Message: {ex.Message}", downloadData.FileIdentifier, ex.Message);
                throw;
            }
        }

    }
}
