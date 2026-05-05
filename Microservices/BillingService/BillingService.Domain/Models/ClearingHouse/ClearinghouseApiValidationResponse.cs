using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BillingService.Domain.Models.ClearingHouse
{
    public class ClearinghouseApiValidationResponse
    {
        public bool AllValid { get; set; }
        public int TotalClearinghouses { get; set; }
        public int SuccessfulValidations { get; set; }
        public int FailedValidations { get; set; }
        public string ErrorMessage { get; set; }

        // matches JSON: "results": [...]
        [JsonPropertyName("results")]
        public List<ClearinghouseCredentialValidationResult> clearinghouseCredentialValidationResults { get; set; } = new();

      
    }
}
