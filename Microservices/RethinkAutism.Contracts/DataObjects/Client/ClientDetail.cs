
namespace RethinkAutism.Contracts.DataObjects.Client
{
    public class ClientDetail
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientInitials { get; set; }
        public string ClientShortName { get; set; }
        public string ClientContactName { get; set; }
        public string ClientContactRelationship { get; set; }
        public int ClientContactId { get; set; }
        public string ClientLocation { get; set; }
        public bool? IsDemoClient { get; set; }
        public bool? IsClientDemo { get; set; }
    }
}
