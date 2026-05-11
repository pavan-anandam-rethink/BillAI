using ClearingHouseService.Domain.Entities;

namespace ClearingHouseService.Application.DTOs
{
    /// <summary>
    /// Result of a claim submission operation through the orchestrator.
    /// </summary>
    public class ClaimSubmissionResult
    {
        public bool IsSuccess { get; set; }
        public string? FileName { get; set; }
        public string? ErrorMessage { get; set; }
        public TransmissionErrorType ErrorType { get; set; } = TransmissionErrorType.None;
        public Guid? TransactionId { get; set; }
        public string? CorrelationId { get; set; }

        public static ClaimSubmissionResult Success(string fileName, Guid? transactionId = null, string? correlationId = null)
        {
            return new ClaimSubmissionResult
            {
                IsSuccess = true,
                FileName = fileName,
                TransactionId = transactionId,
                CorrelationId = correlationId
            };
        }

        public static ClaimSubmissionResult Fail(TransmissionErrorType errorType, string message)
        {
            return new ClaimSubmissionResult
            {
                IsSuccess = false,
                ErrorType = errorType,
                ErrorMessage = message
            };
        }
    }
}
