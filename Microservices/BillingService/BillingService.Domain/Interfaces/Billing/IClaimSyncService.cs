using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Models;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimSyncService
    {
        Task SyncClaimAsync(int appointmentId, int accountInfoId, bool processingSchedule = false);
        Task SyncClaimDeleteAsync(int appointmentId);
        Task<BillingCodeData> GetProviderBillingCode(AppointmentRethinkModel appointment, AppointmentClientAuthBillingCodeModel childProfileAuthorizationBillingCode);
        Task<string> AddDiagnosisCodes(ClaimEntity claim, AppointmentClientAuthBillingCodeModel childProfileAuthorizationBillingCode, int? serviceId);
        Task PublishUnbilledAppointmentForClaimProcessingAsync(int accountInfoId, int memberId, int apptId);
        Task AutoProcessUnBilledAppointmentScheduleBatchAsync();
    }
}