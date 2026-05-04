using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ChildProfileRefererringProvidersModel
    {
        public ChildProfileRefererringProviders childProfileReferringProviders { get; set; }
    }
    [Owned]
    public class ChildProfileRefererringProviders
    {
        public int total { get; set; }
        public List<clientReferringProviders> data { get; set; }
    }
    [Owned]
    public class clientReferringProviders
    {
        public int childProfileId { get; set; }
        public int referringProviderId { get; set; }
        public bool isDefault { get; set; }
        public bool isActive { get; set; }
        public int id { get; set; }
        public ReferringProvidersModel ReferringProvider { get; set; }
        public MetaData? metaData { get; set; }
    }
}
