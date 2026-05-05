using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models
{
    public class ClaimUpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class ClaimPrimaryFunderUpdateException : Exception
    {
        public ClaimPrimaryFunderUpdateException(string message) : base(message) { }
    }
}
