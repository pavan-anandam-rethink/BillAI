using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Files;
using ClearingHouseService.Web.Interface;
using Rethink.Services.Common.Enums.Billing;
using ClearingHouseService.Web.Models;
using Rethink.Services.Common.Services;
using System.Text;
using Billing.FolderStructure.Core.Models;

namespace ClearingHouseService.Web.Service
{
    public class EdiUploadService : BaseService, IEdiUploadService
    {
        private readonly ILogger<EdiUploadService> _logger;
        private readonly ICommon _commonService;
        private readonly IBillingBlobService _billingBlobService;
        private readonly IBillingFilePath _billingFilePath;
        private readonly IClearingHouseUploaderFactory _clearingHouseUploaderFactory;
        private readonly IEdiFilesDownload _ediFilesDownload;
        private readonly IClaimSubmissionHandler _claimSubmissionHandler;

        public EdiUploadService(
            ILogger<EdiUploadService> logger,
            ICommon commonService,
            IBillingBlobService billingBlobService,
            IBillingFilePath billingFilePath,
            IClearingHouseUploaderFactory clearingHouseUploaderFactory,
            IEdiFilesDownload ediFilesDownload,
            IClaimSubmissionHandler claimSubmissionHandler)
        {
            _logger = logger;
            _commonService = commonService;
            _billingBlobService = billingBlobService;
            _billingFilePath = billingFilePath;
            _clearingHouseUploaderFactory = clearingHouseUploaderFactory;
            _ediFilesDownload = ediFilesDownload;
            _claimSubmissionHandler = claimSubmissionHandler;
        }

        public async Task<OperationResult> ProcessClaimAsync(int claimId, string ediData, int clearinghouseId)
        {
            OperationResult uploadResult = null;
            try
            {
                _logger.LogInformation($"ProcessClaimAsync Starting upload for claimId: {claimId} to clearing house Id: {clearinghouseId}");

               uploadResult = await UploadAsync(clearinghouseId, claimId, ediData);

                await _claimSubmissionHandler.HandleUploadResultAsync(claimId, uploadResult);

                _logger.LogInformation($"ProcessClaimAsync End upload for claimId: {claimId} to clearing house Id: {clearinghouseId}");
                return uploadResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading claim: " + $"claimId={claimId} " + $"Error: {ex.Message}");

                // Handle exception by updating claim status to SubmissionFailed
                uploadResult = OperationResult.Fail(ErrorType.Unknown, ex.Message);
                await _claimSubmissionHandler.HandleUploadResultAsync(claimId, uploadResult);
                return uploadResult;
            }
        } 

        private async Task<OperationResult> UploadAsync(int clearinghouseId, int claimId, string ediData)
        {
            var clearingHouse = await _commonService.GetclearinghouseNameById(clearinghouseId);
            var clearingHouseTitle = clearingHouse?.Title ?? $"Unknown({clearinghouseId})";

            var transactionControlResult = await _billingFilePath.GetTransactionControlNumber(ediData);
            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(transactionControlResult);
            var claimNumber = result != null
                ? $"{result.Id}_{EstDateTime:yyMM}.edi"
                : $"{transactionControlResult.ControlNumbers.FirstOrDefault()}_{EstDateTime:yyMM}.edi";

            if (transactionControlResult.FileType != ((int)FileTypes.Type270).ToString())
            {
                int accountInfoId = result != null
                ? result.Claim.AccountInfoId
                : 0;

                _logger.LogInformation($"Uploading EDI file for ClaimId: {claimId} with Claim Number: {claimNumber} to clearing house: {clearingHouseTitle}");

                if (result == null)
                {
                    _logger.LogInformation($"Claim entity not found for transaction number: {claimNumber}. ClaimId: {claimId}");
                    return OperationResult.Fail(ErrorType.ClaimNotFound, "Claim entity not found");
                }

                _logger.LogInformation($"Starting '{claimNumber}' file upload to clearing house: {clearingHouseTitle}. ClaimId: {claimId}");
                byte[] data = Encoding.UTF8.GetBytes(ediData);

                var billingRequest = new BillingRequest
                {
                    FieldIdentifier = claimNumber,
                    FolderName = BlobFolderNames.Outgoing.ToString(),
                    AccountInfoId = accountInfoId,
                    Data = data,
                    BillingContainerName = BillingConstants.BillingContainerName,
                    TransactionNumber = result?.Id,
                    SubFolderName = null,
                    ClearingHouseTitle = clearingHouseTitle,
                    ClearingHouseId = clearinghouseId
                };

                var billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
                var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
                await _ediFilesDownload.SaveClaimEdiFilePath(billingRequest, fullFilePath, result);
                await _billingBlobService.UploadIntoContainerAsync(BillingConstants.BillingContainerName, fullFilePath, new MemoryStream(data));
            }

            // SFTP upload for Stedi and other clearing houses
            await using var ediStream = new MemoryStream();
            await using var streamWriter = new StreamWriter(ediStream);
            await streamWriter.WriteAsync(ediData);
            await streamWriter.FlushAsync();
            ediStream.Position = 0;

            var uploader = _clearingHouseUploaderFactory.Create();
            return await uploader.UploadFileToSftpAsync(clearingHouse, claimNumber, ediStream, claimId);
        }

        /// <summary>
        /// Validates SFTP credentials for all active clearinghouses defined in BillingClearingHousesEnum
        /// </summary>
        public async Task<ClearinghouseCredentialValidationResponse> ValidateAllClearinghousesAsync()
        {
            var validationId = Guid.NewGuid().ToString("N");

            _logger.LogInformation("Starting clearinghouse credentials validation. ValidationId={ValidationId}",validationId);

            var response = new ClearinghouseCredentialValidationResponse
            {
                ValidationTimestamp = DateTime.UtcNow
            };

            try
            {
                // Build clearinghouse list from all enum values using GetclearinghouseNameById
                var clearinghouses = new List<ClearingHouseDetailsModel>();
                foreach (var enumValue in Enum.GetValues<BillingClearingHousesEnum>())
                {
                    var ch = await _commonService.GetclearinghouseNameById((int)enumValue);
                    if (ch != null)
                    {
                        clearinghouses.Add(ch);
                    }
                }

                if (!clearinghouses.Any())  
                {
                    _logger.LogWarning("No active clearinghouses found for validation. ValidationId={ValidationId}",validationId);
                    response.AllValid = true;
                    return response;
                }

                response.TotalClearinghouses = clearinghouses.Count;

                _logger.LogInformation("Validating {Count} clearinghouses. ValidationId={ValidationId}, Clearinghouses={ClearinghouseNames}",clearinghouses.Count,validationId,
                    string.Join(", ", clearinghouses.Select(c => c.Title)));

                // Call SftpUploader to validate the clearinghouses
                var uploader = _clearingHouseUploaderFactory.Create();
                var results = await uploader.ValidateMultipleClearinghousesAsync(clearinghouses);

                response.Results = results;
                response.SuccessfulValidations = results.Count(r => r.IsValid);
                response.FailedValidations = results.Count(r => !r.IsValid);
                response.AllValid = response.FailedValidations == 0;

                _logger.LogInformation("Clearinghouse validation completed. ValidationId={ValidationId}, Total={Total}, Success={Success}, Failed={Failed}",
                    validationId,response.TotalClearinghouses,response.SuccessfulValidations,response.FailedValidations);

                if (!response.AllValid)
                {
                    var failedClearinghouses = results.Where(r => !r.IsValid).Select(r => r.ClearinghouseName);
                    _logger.LogWarning("Validation failures detected. ValidationId={ValidationId}, FailedClearinghouses={FailedNames}",validationId,string.Join(", ", failedClearinghouses));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Critical error during clearinghouse validation. ValidationId={ValidationId}",validationId);
                throw;
            }
        }
    }
}
