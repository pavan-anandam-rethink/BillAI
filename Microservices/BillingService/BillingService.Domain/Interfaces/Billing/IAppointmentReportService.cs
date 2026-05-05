using BillingService.Domain.Models;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ReportingModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IAppointmentReportService
    {
        Task<AppointmentModelWithCount> GetUnbilledAppointmentDetails(UnbilledAppointmentsRequestModel model);
        Task<bool> CreateClaimsForUnbilledAppointmentsAsync(int accountInfoId, int memberId, int[] apptId);
        Task<byte[]> ExportUnbilledAppointmentDataAsync(ExportModelForUnbilledAppointments exportModel);
        Task<byte[]> ExportUnprocessedAppointmentDataAsync(ExportModelForUnprocessedAppointments exportModel);
        Task<AppointmentModelWithCount> UnprocessedAppointments(UnprocessedAppointmentsRequestModel model);
        Task<int> UnprocessedAppointmentsCountAsync(int accountInfoId);

    }
}
