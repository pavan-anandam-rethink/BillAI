using ClearingHouseService.Domain.Entities;

namespace ClearingHouseService.Application.DTOs
{
    /// <summary>
    /// Result of an eligibility check operation through the orchestrator.
    /// </summary>
    public class EligibilityResult
    {
        public bool IsSuccess { get; set; }
        public string? Edi271Response { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid? TransactionId { get; set; }
        public string? CorrelationId { get; set; }

        public static EligibilityResult Success(string edi271Response, Guid? transactionId = null)
        {
            return new EligibilityResult
            {
                IsSuccess = true,
                Edi271Response = edi271Response,
                TransactionId = transactionId
            };
        }

        public static EligibilityResult Fail(string errorMessage)
        {
            return new EligibilityResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
