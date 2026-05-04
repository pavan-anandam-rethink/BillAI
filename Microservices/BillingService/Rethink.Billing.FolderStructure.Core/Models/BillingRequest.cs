namespace Billing.FolderStructure.Core.Models
{
    public class BillingRequest
    {
        public string FieldIdentifier { get; set; }
        public string FolderName { get; set; }
        public int? AccountInfoId { get; set; }
        public byte[] Data { get; set; }
        public string? BillingContainerName { get; set; }
        public int? TransactionNumber { get; set; }
        public string? SubFolderName { get; set; }
        public string? ClearingHouseTitle { get; set; }
        public int? ClearingHouseId { get; set; }
        public int? PaymentId { get; set; }
    }
}
