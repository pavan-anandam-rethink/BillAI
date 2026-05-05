namespace BillingService.XUnit.Tests.Common.Models
{
    public abstract class ActionBaseResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}
