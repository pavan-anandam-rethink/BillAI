namespace Rethink.Services.Common.Models.Claim
{
    public class ClaimStatusUpdate
    {
        public string BatchId { get; set; }
        public int AccountId { get; set; }
        public int UserId { get; set; }
        public int ClaimId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public int Total { get; set; }
    }
}
