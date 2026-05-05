using System;
using System.Collections.Generic;
using Rethink.Services.Common.Helpers;
using RethinkAutism.Contracts.DataObjects.Curriculum;
using RethinkAutism.Core.Services.Data.Scheduling;

namespace RethinkAutism.Core.Services.Helper
{
    public class WeeklyEventConverter : IEventConverter
    {
        public IList<DayPilotEvent> Convert(AppointmentItem appointment, HashSet<long> occurrenceStartDates, HashSet<long> occurrenceSeriesStartDates, HashSet<long> preventableFunderDates,
                                            int startDay, DateTime? startOn = null, DateTime? endOn = null, double? memberUtcOffset = 0, bool? isAddBubbleHtml = true)
        {
            IList<DayPilotEvent> events = new List<DayPilotEvent>();

            DateTime startDate = appointment.StartDate.Date;
            IList<DayOfWeek> dayOfWeeks = ConvertToDayOfWeeks(appointment.DayTypes);

            if (dayOfWeeks.Count > 0)
            {
                int startDateDayOfWeek = (int)startDate.DayOfWeek >= startDay ? (int)startDate.DayOfWeek : (int)startDate.DayOfWeek + 7;
                int delta = startDay - startDateDayOfWeek;
            }

            DateTime endDate = DateTime.MinValue;

            bool isExistOccurrenceStartDate = false;

            if (appointment.OccurrenceEndDate.HasValue)
            {
                endDate = appointment.OccurrenceEndDate.Value.Date.AddDays(1);
            }

            int interval = 7;

            if (appointment.FrequencyInterval > 0)
            {
                interval *= appointment.FrequencyInterval;
            }

            if (appointment.OccurrenceFrequency > 0 && !appointment.OccurrenceEndDate.HasValue)
            {
                endDate = startDate.AddDays(appointment.OccurrenceFrequency * interval);
                if (memberUtcOffset.HasValue)
                    endDate = endDate.AddMinutes(memberUtcOffset.Value);
            }

            if (endDate == DateTime.MinValue || endDate > AppointmentHelper.EventMaxEndDate)
            {
                // no end date case
                endDate = AppointmentHelper.EventMaxEndDate;
            }

            var appointmentStartDate = appointment.StartDate.Date.AddMinutes(appointment.StartTime);
            var offsetDays = (appointmentStartDate.Date - appointmentStartDate.AddMinutes((double)memberUtcOffset).Date).TotalDays;

            if (endOn != null && endDate > endOn)
            {
                endDate = endOn.Value;
            }

            do
            {
                if (dayOfWeeks.Count == 0)
                {
                    if (occurrenceSeriesStartDates != null)
                    {
                        isExistOccurrenceStartDate = occurrenceSeriesStartDates.Contains(startDate.Date.Ticks);
                    }

                    if ((occurrenceStartDates == null || occurrenceStartDates?.Count == 0 || !occurrenceStartDates.Contains(startDate.Date.Ticks))
                        && (preventableFunderDates == null || !preventableFunderDates.Contains(startDate.Date.Ticks) || appointment.AppointmentTypeId > 1))
                    {
                        if (!isExistOccurrenceStartDate)
                        {
                            var start = startDate.AddMinutes(appointment.ActualStartTime ?? appointment.StartTime);
                            if (startOn == null || start >= startOn)
                            {
                                DayPilotEvent nextEvent = AppointmentHelper.CreateDayPilotEvent(
                                    appointment,
                                    start,
                                    startDate.AddMinutes(appointment.ActualEndTime ?? appointment.EndTime),
                                    isAddBubbleHtml);
                                events.Add(nextEvent);
                            }
                        }
                    }
                }
                else
                {
                    var endDateWithOffset = endDate.AddDays(offsetDays);
                    foreach (DayOfWeek dayOfWeek in dayOfWeeks)
                    {
                        DateTime date = GetNextDate(startDate, dayOfWeek).AddDays(offsetDays);

                        if (occurrenceSeriesStartDates != null)
                        {
                            isExistOccurrenceStartDate = occurrenceSeriesStartDates.Contains(date.Date.Ticks);
                        }

                        var seriesAppointmentStartDate = appointment.StartDate.Date;
                        if ((occurrenceStartDates == null || !occurrenceStartDates.Contains(date.Date.Ticks))
                            && (preventableFunderDates == null || !preventableFunderDates.Contains(date.Date.Ticks) || appointment.AppointmentTypeId > 1)
                            && date >= seriesAppointmentStartDate)
                        {
                            if (date < endDateWithOffset)
                            {
                                if (!isExistOccurrenceStartDate)
                                {
                                    var start = date.AddMinutes(appointment.ActualStartTime ?? appointment.StartTime);
                                    if (startOn == null || start >= startOn)
                                    {
                                        DayPilotEvent nextEvent = AppointmentHelper.CreateDayPilotEvent(
                                            appointment,
                                            start,
                                            date.AddMinutes(appointment.ActualEndTime ?? appointment.EndTime),
                                            isAddBubbleHtml);
                                        events.Add(nextEvent);
                                    }
                                }
                            }
                            // 34712 Appointment Exceeds Auth- Include Start Date in events
                            else if (events.Count == 0 && date < endDate && date.ToShortDateString() == startDate.ToShortDateString())
                            {
                                if (!isExistOccurrenceStartDate)
                                {
                                    var start = date.AddMinutes(appointment.ActualStartTime ?? appointment.StartTime);
                                    if (startOn == null || start >= startOn)
                                    {
                                        DayPilotEvent nextEvent = AppointmentHelper.CreateDayPilotEvent(
                                            appointment,
                                            start,
                                            startDate.AddMinutes(appointment.ActualEndTime ?? appointment.EndTime),
                                            isAddBubbleHtml);
                                        events.Add(nextEvent);
                                    }
                                }
                            }
                        }
                    }
                }

                startDate = startDate.AddDays(interval);
            } while (startDate < endDate);

            return events;
        }

        #region Helpers
        private IList<DayOfWeek> ConvertToDayOfWeeks(IList<int> dayTypes)
        {
            IList<DayOfWeek> dayOfWeeks = new List<DayOfWeek>();

            if (dayTypes != null)
            {
                foreach (int dayType in dayTypes)
                {
                    dayOfWeeks.Add(AppointmentHelper.ConvertToDayOfWeek(dayType));
                }
            }

            return dayOfWeeks;
        }

        private DateTime GetNextDate(DateTime date, DayOfWeek dayOfWeek)
        {
            int daysToNext = ((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7;

            return date.AddDays(daysToNext);
        }
        #endregion
    }
}
