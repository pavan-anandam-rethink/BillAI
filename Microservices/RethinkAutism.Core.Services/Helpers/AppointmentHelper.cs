using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Models;
using RethinkAutism.Contracts.DataObjects.Curriculum;
using RethinkAutism.Contracts.Enums;
using RethinkAutism.Contracts.Enums.Curriculum;
using RethinkAutism.Core.Services.Data.Scheduling;
using RethinkAutism.Core.Services.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rethink.Services.Common.Helpers
{
    public class AppointmentHelper
    {
        private const int NumberOfMinutesInADay = 1440;
        private const string DefaultAutocancelledDescription = "Cancelled - Modified Staff / Client";
        public static string GetStatusName(AppointmentRethinkModel appointment)
        {
            var status = appointment.WorkFlowHistory != null && appointment.WorkFlowHistory.expirationDate == null
                                            ? appointment.WorkFlowHistory : null;

            string statusName = status != null ? status.statusDescription : string.Empty;

            var isAppointmentVerified = IsAppointmentVerified(appointment);
            if (appointment.appointmentTypeId == 1)
            {
                if (isAppointmentVerified)
                {
                    statusName = "Completed";
                }
                else
                {
                    if (status != null && status.statusId == 3) // completed
                    {
                        statusName = "Needs Verification";
                    }
                }
            }
            else
            {
                if (isAppointmentVerified)
                {
                    statusName = "Completed";
                }
            }

            if (!string.IsNullOrEmpty(statusName))
            {
                statusName += appointment.DateDeleted != null ? "(Deleted)" : string.Empty;
            }

            return statusName;
        }

        public static DateTime EventMaxEndDate
        {
            get
            {
                return DateTime.Now.AddYears(_maxEventEndDateYear);
            }
        }

        public static DateTime? Convert(DateTime date)
        {
            return date.Date == DateTime.MinValue ? (DateTime?)null : date;
        }
        private static readonly IEventConverter _singleEventConverter = new SingleEventConverter();
        private static readonly IEventConverter _weeklyEventConverter = new WeeklyEventConverter();
        private static readonly IEventConverter _monthlEventConverter = new MonthlyEventConverter();
        private static readonly int _maxEventEndDateYear = 2;
        public static IList<int> ConvertDayType(AppointmentRethinkModel hcAppointment)
        {
            return ConvertDayType(hcAppointment.occurrenceTypeId, hcAppointment.DayTypes);
        }

        public static IList<int> ConvertDayType(int occurrenceTypeId, int dayTypes)
        {
            IList<int> result = new List<int>();

            if (occurrenceTypeId == 2)
            {
                foreach (DayTypes dayType in Enum.GetValues(typeof(DayTypes)))
                {
                    if (((int)dayType & dayTypes) == (int)dayType)
                    {
                        result.Add((int)dayType);
                    }
                }
            }

            return result;
        }

        public static IEventConverter GetEventConverter(int occurrenceTypeId)
        {
            switch (occurrenceTypeId)
            {
                case (int)OccurrenceTypes.Single:
                    return _singleEventConverter;
                case (int)OccurrenceTypes.Weekly:
                    return _weeklyEventConverter;
                case (int)OccurrenceTypes.Monthly:
                    return _monthlEventConverter;
                default:
                    throw new ApplicationException("Need to define an interface for occurrenceTypeId = " + occurrenceTypeId);
            }
        }

        public static bool IsAppointmentItemVerified(AppointmentItem appointment)
        {
            if (appointment.AdminVerificationDate != null)
            {
                return true;
            }

            var staffVerification = appointment.StaffVerificationDate.HasValue && DateTime.Compare(appointment.StaffVerificationDate.Value, DateTime.MinValue) > 0;
            var clientVerification = appointment.ClientVerificationDate.HasValue && DateTime.Compare(appointment.ClientVerificationDate.Value, DateTime.MinValue) > 0;
            if (appointment.AppointmentTypeId == (int)AppointmentTypes.Billable)
            {
                var parentVerification = appointment.IsParentVerificationRequired == true ? clientVerification : true;
                var sessionNoteEntered = appointment.IsSessionNoteEnteredRequired == true ? (appointment.SessionNoteResponseId > 0) : true;

                if (staffVerification == true
                    && parentVerification == true
                    && sessionNoteEntered == true)
                {
                    return true;
                }
            }
            else
            {
                if (staffVerification)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAppointmentVerified(AppointmentRethinkModel a)
        {
            var accountInfo = a.StaffMember != null && a.StaffMember.Member != null ? a.StaffMember.Member.AccountInfo : null;

            bool isParentVerificationRequired = accountInfo != null ? accountInfo.IsParentVerificationRequired ?? false : false;
            bool isSessionNoteEnteredRequired = accountInfo != null ? accountInfo.IsSessionNoteEnteredRequired ?? false : false;

            return IsAppointmentVerified(a.adminVerificationDate, a.staffVerificationDate, a.clientVerificationDate,
                isParentVerificationRequired, isSessionNoteEnteredRequired,
                a.appointmentTypeId, a.sessionNoteResponseId);
        }

        private static bool IsAppointmentVerified(
            DateTime? adminVerificationDate,
            DateTime? staffVerificationDate,
            DateTime? clientVerificationDate,
            bool? isParentVerificationRequired,
            bool? isSessionNoteEnteredRequired,
            int appointmentTypeId,
            int? sessionNoteResponseId
            )
        {
            if (adminVerificationDate != null)
            {
                return true;
            }

            var staffVerification = staffVerificationDate.HasValue
                && Convert(staffVerificationDate.Value) != null;
            if (appointmentTypeId == 1)
            {
                var parentVerification = isParentVerificationRequired == true ? clientVerificationDate != null && AppointmentHelper.Convert(clientVerificationDate.Value) != null : true;
                var sessionNoteEntered = isSessionNoteEnteredRequired == true ? (sessionNoteResponseId > 0) : true;

                if (staffVerification == true && parentVerification == true && sessionNoteEntered == true)
                {
                    return true;
                }
            }
            else
            {
                if (staffVerification)
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetEVVStatus(int? evvStatusId)
        {
            return evvStatusId != null ? ((EVVStatuses)evvStatusId.Value).ToString() : "Pending";
        }

        public static DayPilotEvent CreateDayPilotEvent(AppointmentItem appointment, DateTime start, DateTime end, bool? isAddBubbleHtml = true)
        {

            if (appointment.StatusId < 4)
            {
                var isAppointmentVerified = IsAppointmentItemVerified(appointment);
                if (appointment.AppointmentTypeId == (int)AppointmentTypes.Billable)
                {
                    if (isAppointmentVerified)
                    {
                        appointment.StatusId = (int)AppointmentStatus.Completed;
                        appointment.StatusName = AppointmentStatus.Completed.GetEnumDescription();
                    }
                    else
                    {
                        if (appointment.StatusId == (int)AppointmentStatus.Completed)
                        {
                            appointment.StatusId = (int)AppointmentStatus.NeedsVerification;
                            appointment.StatusName = AppointmentStatus.NeedsVerification.GetEnumDescription();
                        }
                    }
                }
                else
                {
                    if (isAppointmentVerified)
                    {
                        appointment.StatusId = (int)AppointmentStatus.Completed;
                        appointment.StatusName = AppointmentStatus.Completed.GetEnumDescription();
                    }
                }
            }

            int statusId = appointment.StatusId;
            string statusName = appointment.StatusName;
            var ESTDateTime = GetEasternDateTime(null);

            if (appointment.StatusId == (int)AppointmentStatus.NeedsVerification && start > ESTDateTime)
            {
                statusId = (int)AppointmentStatus.Scheduled;
                statusName = AppointmentStatus.Scheduled.GetEnumDescription();
            }
            else if (appointment.StatusId == (int)AppointmentStatus.Scheduled && start < ESTDateTime)
            {
                statusId = (int)AppointmentStatus.NeedsVerification;
                statusName = AppointmentStatus.NeedsVerification.GetEnumDescription();
            }

            if (appointment.CancellationTypeId == 3)
            {
                statusId = (int)AppointmentStatus.AutoCancelled;
            }

            if (appointment.StatusId == (int)AppointmentStatus.AutoCancelled)
            {
                statusName = DefaultAutocancelledDescription;
            }

            var diff = (end - start);

            if (diff.Ticks < 0)
            {
                end = end.AddDays(1);
                diff = (end - start);
            }

            DayPilotEvent dayPilotEvent = new DayPilotEvent()
            {
                id = appointment.Id,
                text = start.ToString("hh:mm tt") + " - " + end.ToString("hh:mm tt"),
                html = start.ToString("hh:mm tt") + " - " + end.ToString("hh:mm tt"),
                start = start,
                end = end,
                resource = appointment.ClientId.ToString(),
                StatusId = statusId,
                StatusName = statusName,
                ProcedureCodeId = appointment.ProcedureCodeId,
                CheckExceedAuthorizationHours = appointment.CheckExceedAuthorizationHours,
                Minutes = (int?)diff.TotalMinutes,
                Hours = (int?)diff.TotalHours,
                EvvStatusName = GetEVVStatus(appointment.EVVStatusId),
                AppointmentTypeId = appointment.AppointmentTypeId
            };

            dayPilotEvent.bubbleHtml = (isAddBubbleHtml == true) ? GenerateBubbleHtml(appointment, dayPilotEvent) : string.Empty;

            return dayPilotEvent;
        }

        public static DayOfWeek ConvertToDayOfWeek(int dayType)
        {
            switch (dayType)
            {
                case (int)DayTypes.Mon:
                    return DayOfWeek.Monday;
                case (int)DayTypes.Tue:
                    return DayOfWeek.Tuesday;
                case (int)DayTypes.Wed:
                    return DayOfWeek.Wednesday;
                case (int)DayTypes.Thu:
                    return DayOfWeek.Thursday;
                case (int)DayTypes.Fri:
                    return DayOfWeek.Friday;
                case (int)DayTypes.Sat:
                    return DayOfWeek.Saturday;
                case (int)DayTypes.Sun:
                    return DayOfWeek.Sunday;
                default:
                    throw new ApplicationException("Can't convert DayTypes of type id = " + dayType);
            }
        }
        private static DateTime GetEasternDateTime(DateTime? dateTimeEntry)
        {
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(dateTimeEntry ?? DateTime.UtcNow, easternZone);
            return easternTime;
        }

        private static string GenerateBubbleHtml(AppointmentItem appointment, DayPilotEvent dayPilotEvent)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("<strong>Client Name:</strong> {0}<br />", appointment.ClientName));
            sb.AppendLine(string.Format("<strong>Staff Member Name:</strong> {0}<br />", appointment.StaffName));
            sb.AppendLine(string.Format("<strong>Service Line:</strong> {0}<br />", appointment.ServiceName));
            sb.AppendLine(string.Format("<strong>Service:</strong> {0}<br />",
                 appointment.PropagatingServiceEndDate == null
                 || dayPilotEvent.start.Date <= appointment.PropagatingServiceEndDate.Value.Date
                    ? !string.IsNullOrEmpty(appointment.PropagatingServiceName)
                        ? appointment.PropagatingServiceName
                        : appointment.ProviderServiceName
                    : appointment.ProviderServiceName));
            if (appointment.AppointmentTypeId == (int)AppointmentTypes.Billable || appointment.AppointmentTypeId == (int)AppointmentTypes.NonBillable)
            {
                sb.AppendLine(string.Format("<strong>Activity:</strong> {0}<br />", appointment.ActivityTagName));
            }
            sb.AppendLine(string.Format("<strong>Date:</strong> {0}<br />", dayPilotEvent.start.ToShortDateString()));
            sb.AppendLine(string.Format("<strong>Time:</strong> {0}<br />", dayPilotEvent.start.ToString("hh:mm tt") + " - " + dayPilotEvent.end.ToString("hh:mm tt")));
            sb.AppendLine(string.Format("<strong>Supervisor Name:</strong> {0}<br />", appointment.StaffSupervisorName));
            sb.AppendLine(string.Format("<strong>Appointment Status:</strong> {0}<br />", GetStatusDisplayName(appointment.StatusId, dayPilotEvent.StatusName, appointment.CancellationNote)));

            if (!string.IsNullOrEmpty(appointment.StaffVerifiedAddress))
            {
                sb.AppendLine(string.Format("<div><strong>Staff Verification Address:</strong> {0}</div>", appointment.StaffVerifiedAddress));
            }

            if (!string.IsNullOrEmpty(appointment.ParentVerifiedAddress))
            {
                sb.AppendLine(string.Format("<div><strong>Parent Verification Address:</strong> {0}</div>", appointment.ParentVerifiedAddress));
            }

            return sb.ToString();
        }

        private static string GetStatusDisplayName(int statusId, string statusName, string cancellationNote)
        {
            string result = string.IsNullOrEmpty(statusName) ? ((AppointmentStatus)statusId).ToString() : statusName;

            if (statusId == (int)AppointmentStatus.AutoCancelled)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("Archived - ");
                stringBuilder.Append(cancellationNote);
                result = stringBuilder.ToString();
            }

            return result;
        }

        public static int GetEventDurationByMinutes(AppointmentItem appointment, DayPilotEvent ev)
        {
            int start;
            int end;
            int timediff;
            if (ev.Minutes != null && ev.Minutes > 0)
            {
                start = (ev.start.Hour * 60) + ev.start.Minute;
                end = (ev.end.Hour * 60) + ev.end.Minute;
                // 34712 Appointment Exceeds Auth- End Time after 12AM next day add 24 hours to end time
                timediff = end - start;
                if (timediff < 0)
                    timediff = (NumberOfMinutesInADay + end) - start;
                return timediff;
            }

            start = appointment.ActualStartTime ?? appointment.StartTime;
            end = appointment.ActualEndTime ?? appointment.EndTime;

            // End Time after 12AM next day add 24 hours to end time
            timediff = end - start;
            if (timediff < 0)
                timediff = (NumberOfMinutesInADay + end) - start;

            return timediff;
        }

    }
}
