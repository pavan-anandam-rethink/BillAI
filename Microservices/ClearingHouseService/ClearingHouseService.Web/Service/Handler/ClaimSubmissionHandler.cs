using ClearingHouseService.Web.Interface;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Interfaces;

namespace ClearingHouseService.Web.Service.Handler
{
    public class ClaimSubmissionHandler : IClaimSubmissionHandler
    {
        private readonly IClaimRepository _claimRepository;
        private readonly ILogger<ClaimSubmissionHandler> _logger;

        public ClaimSubmissionHandler(IClaimRepository claimRepository, ILogger<ClaimSubmissionHandler> logger)
        {
            _claimRepository = claimRepository;
            _logger = logger;
        }

        public async Task HandleUploadResultAsync(int claimId, OperationResult result)
        {
            if (result.IsSuccess)
            {
                // Upload succeeded - set status to Pending
                await _claimRepository.UpdateClaimDetailsAsync(claimId, ClaimStatus.Pending, null);
                _logger.LogInformation("Claim {ClaimId} successfully uploaded. Status set to Pending.", claimId);
                return;
            }

            // Handle failure cases - set status to Billed and save error to ClaimValidationErrors
            var errorNumber = MapErrorTypeToClaimErrorNumber(result.Error);
            var errorMessage = await _claimRepository.GetErrorMessageAsync(errorNumber);
            
            // Fallback to default message if not found in database
            errorMessage ??= GetDefaultErrorMessage(result.Error, result.ErrorMessage);

            // Update claim status to Billed
            await _claimRepository.UpdateClaimDetailsAsync(claimId, ClaimStatus.SubmissionFailed, errorMessage);

            // Save error to ClaimValidationErrors table
            await SaveClaimValidationErrorAsync(claimId, errorNumber, errorMessage);

            _logger.LogWarning("Claim {ClaimId} upload failed with error {Error}: {ErrorMessage}. Status set to Billed.", 
                claimId, result.Error, errorMessage);
        }

        private async Task SaveClaimValidationErrorAsync(int claimId, ClaimErrorNumber errorNumber, string errorMessage)
        {
            try
            {
                // Get the error message ID from database
                var errorMessageId = await _claimRepository.GetErrorMessageIdAsync(errorNumber);
                if (!errorMessageId.HasValue)
                {
                    _logger.LogWarning("Error message ID not found for ErrorNumber: {ErrorNumber}. Skipping validation error save.", errorNumber);
                    return;
                }

                // Get the latest claim submission ID for this claim
                var claimSubmissionId = await _claimRepository.GetLatestClaimSubmissionIdAsync(claimId);

                // Save the claim validation error
                await _claimRepository.SaveClaimValidationErrorAsync(
                    claimId,
                    claimSubmissionId ?? 0,
                    errorMessageId.Value,
                    errorMessage,
                    ClaimErrorSource.Billing
                );

                _logger.LogInformation("Claim validation error saved for ClaimId: {ClaimId}, ErrorNumber: {ErrorNumber}", claimId, errorNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save claim validation error for ClaimId: {ClaimId}", claimId);
            }
        }

        private static ClaimErrorNumber MapErrorTypeToClaimErrorNumber(ErrorType errorType)
        {
            return errorType switch
            {
                ErrorType.AuthFailure => ClaimErrorNumber.ClearingHouseAuthenticationFailure,      // 3210
                ErrorType.ConnectionFailure => ClaimErrorNumber.ClearingHouseConnectionIssue,     // 3211
                ErrorType.Timeout => ClaimErrorNumber.ClearingHouseConnectionIssue,               // 3211
                ErrorType.UploadFailed => ClaimErrorNumber.ClearingHouseUploadFailed,             // 3212
                ErrorType.FileGenerationFailed => ClaimErrorNumber.ClearingHouseUploadFailed,     // 3212
                ErrorType.InvalidClearingHouseConfig => ClaimErrorNumber.ClearingHouseDetailsMissing,
                ErrorType.ClaimNotFound => ClaimErrorNumber.Unknown,
                ErrorType.ValidationFailed => ClaimErrorNumber.ClearingHouseUploadFailed,         // 3212
                _ => ClaimErrorNumber.ClearingHouseUploadFailed                                   // 3212
            };
        }

        private static string GetDefaultErrorMessage(ErrorType errorType, string originalMessage)
        {
            return errorType switch
            {
                ErrorType.AuthFailure => "Clearing House - Authentication failure",
                ErrorType.ConnectionFailure => "Clearing House - Connection issue",
                ErrorType.Timeout => "Clearing House - Connection issue",
                ErrorType.UploadFailed => "Clearing House - Upload failed",
                ErrorType.FileGenerationFailed => "Clearing House - Upload failed",
                ErrorType.InvalidClearingHouseConfig => "Clearing House - Configuration missing",
                ErrorType.ClaimNotFound => "Claim not found",
                ErrorType.ValidationFailed => "Clearing House - Upload failed",
                _ => originalMessage ?? "Clearing House - Upload failed"
            };
        }
    }
}
