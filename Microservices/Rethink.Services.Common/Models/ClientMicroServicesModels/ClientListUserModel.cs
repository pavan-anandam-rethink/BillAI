using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientListUserModel
    {
        public int total { get; set; }
        public List<ClientUserModel> data { get; set; }
    }
    [Owned]
    public class ClientFacilityModel
    {
        public int providerLocationId { get; set; }
    }
    [Owned]
    public class ChildProfileReferringProviders
    {
        public ClientReferringProvidersModel childProfileReferringProviders { get; set; }
    }
    [Owned]
    public class ClientReferringProvidersModel
    {
        public int total { get; set; }
        public List<ReferringProviders> data { get; set; }
    }
    [Owned]
    public class ReferringProviders
    {
        public int childProfileId { get; set; }
        public int referringProviderId { get; set; }
        public bool isDefault { get; set; }
        public bool isActive { get; set; }
        public int id { get; set; }
    }
    [Owned]
    public class ReferringProviderNameModel
    {
        public int id { get; set; }
        public Name name { get; set; }
    }
    [Owned]
    public class Name
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
    }
}
