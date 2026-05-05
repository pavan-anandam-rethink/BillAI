using System;
using System.Collections.Generic;
using Rethink.Services.Common.Helpers;
using RethinkAutism.Contracts.DataObjects.Curriculum;
using RethinkAutism.Contracts.Enums.Curriculum;
using RethinkAutism.Core.Services.Data.Scheduling;

namespace RethinkAutism.Core.Services.Helper
{
    public class MonthlyEventConverter : IEventConverter
    {
        public IList<DayPilotEvent> Convert(AppointmentItem appointment, HashSet<long> occurrenceStartDates, HashSet<long> occurrenceSeriesStartDates, HashSet<long> preventableFunderDates,
            int startDay, DateTime? startOn = null, DateTime? endOn = null, double? memberESTOffset = 0, bool? isAddBubbleHtml = true)
        {
            IList<DayPilotEvent> events = new List<DayPilotEvent>();

            DateTime startDate = appointment.StartDate.Date;

            // set date to start of month
            startDate = startDate.AddDays(1 - startDate.Day);

            DateTime endDate = DateTime.MinValue;

            bool isExistOccurrenceStartDate = false;

            if (appointment.OccurrenceEndDate.HasValue)
            {
                endDate = appointment.OccurrenceEndDate.Value.AddDays(1);
            }

            if (appointment.OccurrenceFrequency > 0)
            {
                int interval = 1;
                if (appointment.FrequencyInterval > 0)
                {
                    interval = appointment.FrequencyInterval;
                }
                endDate = startDate.AddMonths(appointment.OccurrenceFrequency * interval);
            }

            if (endDate == DateTime.MinValue || endDate > AppointmentHelper.EventMaxEndDate)
            {
                // no end date case
                endDate = AppointmentHelper.EventMaxEndDate;
            }

            var offsetDays = 0; 

            if (endOn != null && endDate > endOn)
            {
                endDate = endOn.Value;
            }

            do
            {
                DayPilotEvent nextEvent = null;

                if (appointment.MonthTypeId == (int)MonthTypes.Day)
                {
                    if (appointment.MonthDay <= 0)
                    {
                        nextEvent = AppointmentHelper.CreateDayPilotEvent(
                            appointment,
                             startDate.AddMinutes(appointment.ActualStartTime ?? appointment.StartTime).AddMinutes(memberESTOffset.Value),
                            startDate.AddMinutes(appointment.ActualEndTime ?? appointment.EndTime).AddMinutes(memberESTOffset.Value),
                            isAddBubbleHtml);
                    }
                    else
                    {
                        // expected day of the month
                        int expectedMonthDay = appointment.MonthDay;

                        if (expectedMonthDay <= 0)
                        {
                            expectedMonthDay = 1;
                        }

                        int monthDays = DateTime.DaysInMonth(startDate.Year, startDate.Month);

                        if (expectedMonthDay > monthDays)
                        {
                            expectedMonthDay = monthDays;
                        }

                        DateTime date = new DateTime(startDate.Year, startDate.Month, expectedMonthDay).AddDays(offsetDays);
                        var startTime = appointment.StartTime + (int)memberESTOffset.GetValueOrDefault(0);
                        var endTime = appointment.EndTime + (int)memberESTOffset.GetValueOrDefault(0);
                        var actualStartTime = appointment.ActualStartTime + (int)memberESTOffset.GetValueOrDefault(0);
                        var actualEndTime = appointment.ActualEndTime + (int)memberESTOffset.GetValueOrDefault(0);

                        nextEvent = AppointmentHelper.CreateDayPilotEvent(
                            appointment,
                            date.AddMinutes(actualStartTime ?? startTime),
                            date.AddMinutes(actualEndTime ?? endTime),
                            isAddBubbleHtml);
                    }
                }
                else if (appointment.MonthTypeId == (int)MonthTypes.Occurrence)
                {
                    MonthOccurrenceTypes occurrenceType = MonthOccurrenceTypes.First;

                    if (appointment.MonthOccurrenceTypeId != null)
                    {
                        occurrenceType = (MonthOccurrenceTypes)appointment.MonthOccurrenceTypeId.Value;
                    }

                    DayOfWeek dayOfWeek = DayOfWeek.Monday;

                    if (appointment.MonthOccurrenceDayId != null)
                    {
                        dayOfWeek = AppointmentHelper.ConvertToDayOfWeek(appointment.MonthOccurrenceDayId.Value);
                    }

                    // set to next day of week
                    int daysUntilDayOfWeek = ((int)dayOfWeek - (int)startDate.DayOfWeek + 7) % 7;
                    DateTime nextDayOfWeek = startDate.AddDays(daysUntilDayOfWeek);

                    for (int numberOfWeeks = 1; numberOfWeeks < 10; numberOfWeeks++)
                    {
                        if (numberOfWeeks == 1 && occurrenceType == MonthOccurrenceTypes.First)
                        {
                            break;
                        }
                        if (numberOfWeeks == 2 && occurrenceType == MonthOccurrenceTypes.Second)
                        {
                            break;
                        }
                        if (numberOfWeeks == 3 && occurrenceType == MonthOccurrenceTypes.Third)
                        {
                            break;
                        }
                        if (numberOfWeeks == 4 && occurrenceType == MonthOccurrenceTypes.Fourth)
                        {
                            break;
                        }

                        if (nextDayOfWeek.Month != startDate.Month && occurrenceType == MonthOccurrenceTypes.Last)
                        {
                            nextDayOfWeek = nextDayOfWeek.AddDays(-7);
                            break;
                        }

                        nextDayOfWeek = nextDayOfWeek.AddDays(7);
                    }

                    nextEvent = AppointmentHelper.CreateDayPilotEvent(
                        appointment,
                        nextDayOfWeek.AddMinutes(appointment.ActualStartTime ?? appointment.StartTime),
                        nextDayOfWeek.AddMinutes(appointment.ActualEndTime ?? appointment.EndTime),
                        isAddBubbleHtml);
                }


                if (nextEvent != null &&
                    (occurrenceStartDates == null || !occurrenceStartDates.Contains(nextEvent.start.Date.Ticks)) &&
                    (preventableFunderDates == null || !preventableFunderDates.Contains(nextEvent.start.Date.Ticks))
                    && nextEvent.start <= endDate)
                {
                    if (occurrenceSeriesStartDates != null)
                    {
                        isExistOccurrenceStartDate = occurrenceSeriesStartDates.Contains(nextEvent.start.Date.Ticks);
                    }

                    if (!isExistOccurrenceStartDate)
                    {
                        if (startOn == null || nextEvent.start >= startOn)
                        {
                            events.Add(nextEvent);
                        }
                    }
                }

                int interval = DateTime.DaysInMonth(startDate.Year, startDate.Month);

                if (appointment.FrequencyInterval > 0)
                {
                    for (int i = 1; i < appointment.FrequencyInterval; i++)
                    {
                        interval += DateTime.DaysInMonth(startDate.Year, startDate.AddMonths(i).Month);
                    }
                }

                startDate = startDate.AddDays(interval);
            } while (startDate < endDate);

            return events;
        }

    }
}
