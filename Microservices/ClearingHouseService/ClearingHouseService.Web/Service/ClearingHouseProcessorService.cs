using BillingService.Domain.Models.Claims;
using ClearingHouseService.Web.Interface;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Services;

namespace ClearingHouseService.Web.Service
{
    public sealed class ClearingHouseProcessorService : BaseService, IClearingHouseProcessor
    {
        private readonly ICommon _commonService;
        private readonly ILogger<ClearingHouseProcessorService> _logger;
        public ClearingHouseProcessorService(ICommon commonservice, ILogger<ClearingHouseProcessorService> logger)
        {
            _commonService = commonservice;
            _logger = logger;
        }

        public async Task<(bool success, string result)> GenerateEDI(ClearingHouseClaimModel claimModelDto)
        {
            try
            {
                _logger.LogInformation("Generating EDI for ClaimId: {ClaimId}, ClearingHouseId: {ClearingHouseId}", claimModelDto.claimId, claimModelDto.clearinghouseId);
                return await _commonService.GenerateEDIData(claimModelDto);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating EDI for ClaimId: {ClaimId}, ClearingHouseId: {ClearingHouseId}", claimModelDto.claimId, claimModelDto.clearinghouseId);
                return (false, ex.Message.ToString());
            }

        }

        public async Task<(bool success, string result)> UploadfileToBlobStorage(ClaimUploadModelWithUserInfo filesWithUserInfo)
        {
            try
            {
                _logger.LogInformation("Uploading file to Blob Storage for FileName: {FileName}, ClaimId: {ClaimId}", filesWithUserInfo.FileName, filesWithUserInfo.ClaimId);
                return await _commonService.UploadfileToBlobStorage(filesWithUserInfo);
                _logger.LogInformation("Successfully uploaded file to Blob Storage for FileName: {FileName}, ClaimId: {ClaimId}", filesWithUserInfo.FileName, filesWithUserInfo.ClaimId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to Blob Storage for FileName: {FileName}, ClaimId: {ClaimId}", filesWithUserInfo.FileName, filesWithUserInfo.ClaimId);
                return (false, ex.Message.ToString());
            }

        }

        public async Task<(bool success, string result)> UploadSFTPfilesToBlobStorage(DownloadSftpDataModel fileStreams)
        {
            _logger.LogInformation(
                "Starting upload of SFTP file to Blob Storage. ClearingHouseId: {ClearingHouseId}, FileName: {FileName}, Title: {Title}",
                fileStreams.clearingHouseId,
                fileStreams.FileName,
                fileStreams.Title);

            try
            {
                var response = await _commonService.UploadSFTPfilesToBlobStorage(fileStreams);

                _logger.LogInformation(
                    "Completed upload of SFTP file to Blob Storage. ClearingHouseId: {ClearingHouseId}, FileName: {FileName}, Success: {Success}",
                    fileStreams.clearingHouseId,
                    fileStreams.FileName,
                    response.success);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error uploading SFTP file to Blob Storage. ClearingHouseId: {ClearingHouseId}, FileName: {FileName}, Message: {Message}",
                    fileStreams.clearingHouseId,
                    fileStreams.FileName,
                    ex.Message);

                return (false, ex.Message);
            }
        }



    }
}
