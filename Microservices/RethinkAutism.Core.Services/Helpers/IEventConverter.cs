using System;
using System.Collections.Generic;
using RethinkAutism.Contracts.DataObjects.Curriculum;
using RethinkAutism.Core.Services.Data.Scheduling;

namespace RethinkAutism.Core.Services.Helper
{
    public interface IEventConverter
    {
        IList<DayPilotEvent> Convert(AppointmentItem appointment, HashSet<long> occurrenceStartDates, HashSet<long> occurrenceSeriesStartDates, HashSet<long> preventableFunderDates,
                                     int startDay, DateTime? startOn = null, DateTime? endOn = null, double? memberUtcOffset = 0, bool? isAddBubbleHtml = true);
    }
}
