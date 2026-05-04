using System;
using System.Collections.Generic;

namespace ClearingHouseService.Web.Models
{
    public class ClearinghouseCredentialValidationResponse
    {
        public bool AllValid { get; set; }
        public int TotalClearinghouses { get; set; }
        public int SuccessfulValidations { get; set; }
        public int FailedValidations { get; set; }
        public List<ClearinghouseCredentialValidationResult> Results { get; set; } = new List<ClearinghouseCredentialValidationResult>();
        public DateTime ValidationTimestamp { get; set; }
    }
}
