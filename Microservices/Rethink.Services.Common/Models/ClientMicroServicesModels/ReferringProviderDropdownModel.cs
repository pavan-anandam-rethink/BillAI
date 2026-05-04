namespace Rethink.Services.Common.Models.Clients
{
    public class ReferringProviderDropdownModel
    {
        public int Id { get; set; }
        public string ProviderName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
    }
}
