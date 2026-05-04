
using Rethink.Services.Common.Enums.BH;
using System;
using System.Collections.Generic;
using System.Text;

namespace RethinkAutism.Contracts.DataObjects.Billing
{
    public class BillingCodeDetail
    {
        public int Id { get; set; }
        public string FunderName { get; set; }
        public string ServiceName { get; set; }
        public string ProviderServiceName { get; set; }
        public string AuthorizationNumber { get; set; }
        public decimal? AuthorizationUsedHours { get; set; }
        public int? AuthorizationDistributionTypeId { get; set; }
        public int? NoOfUnits { get; set; }
        public int? Unit { get; set; }
        public FrequencyTypes? FrequencyType { get; set; }
        public int? AuthorizedTotalMinutes { get; set; }
        public int? AuthorizationFrequencyType { get; set; }
        public int? ChildProfileAuthorizationId { get; set; }
        public FrequencyTypes? SchedulingGoalFrequencyTypeId { get; set; }
        public int? SchedulingGoalNoOfUnits { get; set; }
        public int? UnitSchedulingGoal { get; set; }
        public bool? IsSameAuthorizaitonAndSchedulingGoal { get; set; }
        public DateTime AuthorizationStartDate { get; set; }
        public DateTime AuthorizationEndDate { get; set; }
        public DateTime? AuthorizationInactiveDate { get; set; }
        public string BillingCode { get; set; }
        public string Modifier1 { get; set; }
        public string Modifier2 { get; set; }
        public string Modifier2Name { get; set; }
        public int RenderingProviderTypeId { get; set; }
        public string RenderingProvider { get; set; }
        public int ProviderBillingCodeCredentialId { get; set; }
        public int CredentialId { get; set; }
        public decimal ContractRate { get; set; }
        public bool IsEVV { get; set; }
        public bool IsMissingBillingInfo { get; set; }
        public bool IsPendingSubmission { get; set; }
        public bool NoAuthRequired { get; set; }

        public List<string> CredentialsModifiers1 { get; set; }
        public List<string> CredentialsModifiers2 { get; set; }
        public int ClientId { get; set; }
        public int FunderId { get; set; }

        public IEnumerable<string> Modifiers1 { get; set; }
        public IEnumerable<string> Modifiers2 { get; set; }
    }

    public class ExceedAuthorizationEvent
    {
        public IDictionary<int, List<DateTime>> ExceedAuthorizationEvents { get; set; }
        public IDictionary<int, List<DateTime>> ExceedSchedulingGoalEvents { get; set; }
    }
}
