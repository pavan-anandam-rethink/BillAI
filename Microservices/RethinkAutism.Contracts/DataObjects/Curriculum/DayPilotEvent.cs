using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace RethinkAutism.Contracts.DataObjects.Curriculum
{
    public class DayPilotEvent 
    {
        public int id { get; set; }
        public string text { get; set; }
        public string html { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string resource { get; set; }
        public string bubbleHtml { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public string EvvStatusName { get; set; }
        public bool? CheckExceedAuthorizationHours { get; set; }
        public int? ProcedureCodeId { get; set; }
        public int? Minutes { get; set; }
        public int? Hours { get; set; }
        public bool? isExceedAuthorizationHours { get; set; }
        public bool? isExceedSchedulingGoal { get; set; }
        public int CancellationTypeId { get; set; }
        public List<string> ValidationReasons { get; set; }
        public string CssClass { get; set; }
        public bool ExceedAuthorizedHours { get; set; }
        public int? ClientId { get; set; }
        public int StaffId { get; set; }
        public int OccurrenceTypeId { get; set; }
        public int AppointmentTypeId { get; set; }
        public string Validation { get; set; }
        public double AuthorizedWeeklyHours { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public int? ActualStartTime { get; set; }
        public int? ActualEndTime { get; set; }
        public string ClientName { get; set; }
        public string ClientInitials { get; set; }
        public string ProviderServiceName { get; set; }
        public string PropagatingServiceName { get; set; }
        public string ActivityTagName { get; set; }
        public DateTime? PropagatingServiceEndDate { get; set; }
        public string StaffName { get; set; }
        public string StaffInitials { get; set; }
        public DateTime DateLastModified { get; set; }
        public bool FunderIsActive { get; set; }

        public void WriteJson(JsonTextWriter writer)
        {
            
        }
    }
}
