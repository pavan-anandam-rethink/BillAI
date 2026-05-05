using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IAppointmentService
    {
        Task<List<AppointmentModel>> GetFor(int accountInfoId, int currentMemberId, int claimId, int? clientId, int? memberId, DateTime? startDate, DateTime? endDate, int? locationId);
        Task<List<AppointmentModel>> GetForClaim(int accountInfoId, int memberId, int claimId);
        Task<List<ClaimsAssigneeResponse>> GetClaimsAssignees(ClaimFilterGetModel model);
        Task<(bool, DateTime?, DateTime?)> LinkAppointments(int accountInfoId, int memberId, int claimId, List<int> appointmentIds);
        Task<(bool, DateTime?, DateTime?)> UnLinkAppointments(int accountInfoId, int memberId, int claimId, List<int> appointmentIds);       
        Task<List<AppointmentModel>> ToAppointmentItems(int accountInfoId, List<AppointmentRethinkModel> appointments, int memberId);
        Task<List<AppointmentRethinkModel>> SetupRethinkDataForAppointments(List<AppointmentRethinkModel> appointmentList);
    }
}
