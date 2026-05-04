using System;

namespace Rethink.Services.Common.Dtos.Billing
{
   
    public class Eligibility270DTO
    {
        public int? AccountInfoId { get; set; }

        public string? ChildProfileReferringProviderId { get; set; }

        public int ClientFunderId { get; set; }

        public int? ClientId { get; set; }
        public string ClientName { get; set; }
        public string? PayerId { get; set; }

        public string FunderName { get; set; }

        public int Id { get; set; }

        public string? ChildProfileReferringProviderName { get; set; }

        public int? ChildProfileRenderingProviderId { get; set; }

        public string? ChildProfileRenderingProviderName { get; set; }

        public string? StaffMemberNpiNumber { get; set; }

        public string? ClearingHousePayerName { get; set; }

        public DateTime EffectiveDate { get; set; }
        public DateTime? DOB { get; set; }
        public int? GenderId { get; set; }
        public string SubscriberId { get; set; }
    }
}
