using BillingService.Domain.Models.Funders;
using Rethink.Services.Common.Enums.BH;
using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models.Clients
{
    public class ClientFunderModel
    {
        public int Id { get; set; }
        public int FunderId { get; set; }
        public string FunderName { get; set; }
        public FunderType FunderType { get; set; }
        public List<FunderServiceLineModel> ServiceLines { get; set; }
        public bool ReferringProviderRequiredOnClaim { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public int? BillingProviderOptionId { get; set; }
    }
}