using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Claim
{


    public class claimFunderSearch : BasePersistEntity
    {
        public string Name { get; set; }
        public DateTime? DateDeleted { get; set; }
    }

    public class ClaimSearchFunderEntity : claimFunderSearch
    { }

    public class ClaimSearchClientEntity : BasePersistEntity
    {
        public string lastName { get; set; }
        public string? firstName { get; set; }
        public string? middleName { get; set; }
        public DateTime? DateDeleted { get; set; }
    }

    public class ClaimSearchRenderingProviderEntity : claimFunderSearch
    { }

    public class ClaimSearchLocationEntity : claimFunderSearch
    { }

    public class ClaimSearchChildProfileAuthorizationEntity : BasePersistEntity
    {
        public DateTime? DateDeleted { get; set; }
        public int? ChildProfileFunderId { get; set; }
        public int FunderId { get; set; }
    }

    public class ClaimSearchTablesUpdateLog : BasePersistEntity
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public string Callstack { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
