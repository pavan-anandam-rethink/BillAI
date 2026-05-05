namespace BillingService.Domain.Models
{
    public class ClaimErrorsSourcesModel
    {
        public string[] ErrorsSources { get; set; }
    }

    public class ClaimErrorsCodesModel
    {
        public ClaimErrorsCodes[] ErrorsCodes { get; set; }
    }

    public class ClaimErrorsCodes
    {
        public string Name { get; set; }
        public bool Checked { get; set; } = false;
    }

    public class ClaimApprovalResponseModel
    {
        public int Claimid { get; set; }
        public string? Error { get; set; }
    }
}
