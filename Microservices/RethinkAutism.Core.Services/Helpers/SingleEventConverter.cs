using System;
using System.Collections.Generic;
using Rethink.Services.Common.Helpers;
using RethinkAutism.Contracts.DataObjects.Curriculum;
using RethinkAutism.Core.Services.Data.Scheduling;

namespace RethinkAutism.Core.Services.Helper
{
    public class SingleEventConverter : IEventConverter
    {
        public IList<DayPilotEvent> Convert(AppointmentItem appointment, HashSet<long> occurrenceStartDates, HashSet<long> occurrenceSeriesStartDates, HashSet<long> preventableFunderDates,
                                            int startDay, DateTime? startOn = null, DateTime? endOn = null, double? memberUtcOffset = 0, bool? isAddBubbleHtml = true)
        {
            return new DayPilotEvent[]
            {
                AppointmentHelper.CreateDayPilotEvent(appointment, appointment.StartDate.Date.AddMinutes(appointment.ActualStartTime ?? appointment.StartTime), appointment.StartDate.Date.AddMinutes(appointment.ActualEndTime ?? appointment.EndTime), isAddBubbleHtml)
            };
        }
    }
}
