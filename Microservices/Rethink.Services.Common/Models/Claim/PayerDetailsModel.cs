namespace Rethink.Services.Common.Models.Claim
{
    public class PayerDetailsModel
    {
        public string payerId { get; set; }
        public string payerName { get; set; }
        public string street1 { get; set; }
        public string street2 { get; set; }
        public string city { get; set; }
        public string stateName { get; set; }
        public int? stateId { get; set; }
        public string zip { get; set; }
        public string phone { get; set; }
        public string fax { get; set; }
        public string email { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }


}
