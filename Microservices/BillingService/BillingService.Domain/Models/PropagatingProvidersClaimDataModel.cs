using System;

namespace BillingService.Domain.Models
{
    public class PropagatingProvidersClaimDataModel : UserInfo
    {
        public ClientAuthorizationBilling Billing { get; set; }
        public int AuthorizationId { get; set; }
    }

    public class ClientAuthorizationBilling
    {
        public int? ReferringProviderId { get; set; }
        public int? ServiceProviderId { get; set; }
        public int? BillingProviderId { get; set; }
        public PropagatingAppointmentData ReferringPropagatingData { get; set; } = null;
        public PropagatingAppointmentData ServicePropagatingData { get; set; } = null;
        public PropagatingAppointmentData BillingPropagatingData { get; set; } = null;
        public int? RenderingProviderId { get; set; }
        public int? RenderingProviderTypeId { get; set; }
        public PropagatingAppointmentData RenderingPropagatingData { get; set; } = null;
    }

    public class PropagatingAppointmentData
    {
        public Type? TypeId { get; set; }
        public DateTime? StartDate { get; set; }

        public enum Type
        {
            ApplyToNewAndExistingAppointments = 1,
            ApplyToNewAndExistingAppointmentsAfterDate = 2,
            ApplyToNewAppointments = 3

        }
    }
}
