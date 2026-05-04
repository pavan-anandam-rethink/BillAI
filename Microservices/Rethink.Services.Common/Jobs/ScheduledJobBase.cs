using Microsoft.Extensions.Logging;
using Quartz;
using Rethink.Services.Common.Extensions;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Jobs
{
    public enum ScheduledJobInterval
    {
        EveryXMinutes,
        EveryXHours,
        Daily,
        MonThruFri,
        MonThruSat,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday,
        LastDayOfMonth,
    }

    /// <summary>
    /// This is the base class for all jobs. It detects when it is running under the debugger and prevents
    /// scheduling of jobs. To debug a job, set the Debug = true on the job and it will schedule to run
    /// within 1 minute of launch.
    ///
    /// NOTE: if a job is detected with Debug = true and the application is not running under a debugger, the system will
    /// throw an exception. In other words, do not check in with Debug = true!
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ScheduledJobBase<T> : IScheduledJob
        where T : IJob
    {
        //private TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

        private readonly string _cronSchedule;
        private readonly string _identityName;
        private readonly string _group;
        private readonly ScheduledJobInterval _jobInterval;
        private readonly int _increment;

        private ILogger _logger;

        private bool _isBuilt = false;
        private IJobDetail _jobDetail;
        private ITrigger _trigger;

        public ScheduledJobBase(string identityName,
                                string group,
                                ScheduledJobInterval jobInterval,
                                int increment,
                                ILogger logger)
        {
            _identityName = identityName;
            _group = group;
            _jobInterval = jobInterval;
            _increment = increment;
            _logger = logger;
        }

        public ScheduledJobBase(string identityName,
                                string group,
                                string cronSchedule,
                                ILogger logger)
        {
            _identityName = identityName;
            _group = group;
            _cronSchedule = cronSchedule;
            _logger = logger;
        }

        public IJobDetail JobDetail
        {
            get
            {
                CheckBuilt();
                return _jobDetail;
            }
        }

        public ITrigger Trigger
        {
            get
            {
                CheckBuilt();
                return _trigger;
            }
        }

        public bool Debug { get; set; } = false;
        public bool IsActive { get; private set; } = true;

        public void Build(ILogger logger)
        {
            _logger = logger;

            _jobDetail = BuildJobDetail(_identityName, _group);

            _trigger = string.IsNullOrWhiteSpace(_cronSchedule) ? BuildTrigger(_jobInterval, _increment) :
                       _cronSchedule.Equals("now", StringComparison.InvariantCultureIgnoreCase) ? BuildImmediateFireTrigger() :
                       _cronSchedule.Equals("never", StringComparison.InvariantCultureIgnoreCase) ? BuildNeverGonnaFireTrigger()
                                                                                                  : BuildTrigger(_cronSchedule);
            _isBuilt = true;
        }


        //======================================================================================
        // private
        //======================================================================================

        private IJobDetail BuildJobDetail(string identityName, string group)
        {
            if (_isBuilt)
            {
                return _jobDetail;
            }

            return JobBuilder.Create<T>()
                .WithIdentity(identityName, group)
                .Build();
        }

        private ITrigger BuildTrigger(ScheduledJobInterval jobInterval, int increment)
        {
            return BuildTrigger(jobInterval, increment, null);
        }
        private ITrigger BuildTrigger(string cronSchedule)
        {
            return BuildTrigger(null, null, cronSchedule);
        }

        private ITrigger BuildTrigger(ScheduledJobInterval? jobInterval, int? increment, string cronSchedule)
        {
            if (_isBuilt)
            {
                return _trigger;
            }

            if (Debugger.IsAttached)
            {
                IsActive = false;

                if (Debug)
                {
                    IsActive = true;
                    var debugTrigger = BuildDebugTrigger();// fire immediately
                    Log($"WILL RUN (debug on) at {GetPrintDate(debugTrigger.StartTimeUtc.LocalDateTime)}");
                    return debugTrigger;
                }
                else
                {
                    LogWarn($"DISABLED (debug off)");
                    return BuildNeverGonnaFireTrigger(); // the name says it all
                }
            }
            else // debugger is not attached
            {
                IsActive = true;
                // if debug is set to true, there is an error. Technically, this would work, but it 
                // means that something was not caught in code review so kill the process.
                if (Debug == true)
                {
                    var msg = $"Invalid scheduled job configuration detected. Debug = true for Scheduled Job [{GetType().Name}]. Turn Debug off and recompile/redeploy.";
                    LogError(msg);
                    throw new ConfigurationErrorsException($"{GetType().Name}: {msg}");
                }
            }

            if (!string.IsNullOrWhiteSpace(cronSchedule))
            {
                var cronTrigger = BuildCronTrigger(cronSchedule);
                Log($"Scheduled (cron '{cronSchedule}'):  NextRun: {GetPrintDateFromUtc(cronTrigger.StartTimeUtc.DateTime)}");
                return cronTrigger;
            }

            if (!jobInterval.HasValue || !increment.HasValue)
            {
                var msg = "(jobInterval, increment) or cronSchedule must be passed to build a trigger";
                LogError(msg);
                throw new ArgumentException($"{GetType().Name}: {msg}");
            }

            string atOrEvery = "at";

            switch (jobInterval.Value)
            {
                case ScheduledJobInterval.EveryXMinutes:
                case ScheduledJobInterval.EveryXHours:
                    atOrEvery = "every";
                    break;
            }

            ITrigger trigger = null;
            switch (jobInterval.Value)
            {
                case ScheduledJobInterval.EveryXMinutes:
                    trigger = BuildMinuteTrigger(increment.Value);
                    break;
                case ScheduledJobInterval.EveryXHours:
                    trigger = BuildHourlyTrigger(increment.Value);
                    break;
                case ScheduledJobInterval.Daily:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} ? * *"); // At <increment>:00:00 every day
                    break;
                case ScheduledJobInterval.MonThruFri:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} ? * MON-FRI"); // At <increment>:00:00 mon-fri
                    break;
                case ScheduledJobInterval.MonThruSat:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} ? * MON-SAT"); // At <increment>:00:00 mon-sat
                    break;
                case ScheduledJobInterval.Monday:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} ? * MON"); // At <increment>:00:00 monday
                    break;
                case ScheduledJobInterval.Tuesday:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} ? * TUE"); // At <increment>:00:00 tuesday
                    break;
                case ScheduledJobInterval.Wednesday:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} ? * WED"); // At <increment>:00:00 wednesday
                    break;
                case ScheduledJobInterval.Thursday:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} ? * THU"); // At <increment>:00:00 thursday
                    break;
                case ScheduledJobInterval.Friday:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} ? * FRI"); // At <increment>:00:00 friday
                    break;
                case ScheduledJobInterval.Saturday:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} ? * SAT"); // At <increment>:00:00 saturday
                    break;
                case ScheduledJobInterval.Sunday:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} ? * SUN"); // At <increment>:00:00 sunday
                    break;
                case ScheduledJobInterval.LastDayOfMonth:
                    trigger = BuildCronTrigger($"0 0 {increment.Value} L * ?"); // At 20:00:00, on the last day of the month, every month
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(jobInterval), jobInterval, $"{GetType().Name}: Unknown job interval");
            }


            var nextFireTimeOffset = trigger.GetFireTimeAfter(DateTime.Now);
            DateTime nextFireTime = nextFireTimeOffset?.LocalDateTime ?? trigger.StartTimeUtc.LocalDateTime;
            Log($"Scheduled ({jobInterval} {atOrEvery} {increment}). NextRun: {GetPrintDateFromUtc(nextFireTime)}");

            return trigger;
        }

        private ITrigger BuildMinuteTrigger(int minutes)
        {
            return TriggerBuilder.Create()
                .WithIdentity(Guid.NewGuid().ToString("N"))
                .StartAt(DateTimeExt.EvenIncrementDateAfterNow(minutes))
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(minutes)
                    .RepeatForever())
                .Build();
        }

        private ITrigger BuildHourlyTrigger(int hours)
        {
            return TriggerBuilder.Create()
                .WithIdentity(Guid.NewGuid().ToString("N"))
                .StartAt(DateBuilder.EvenHourDateAfterNow())
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(hours)
                    .RepeatForever())
                .Build();
        }

        private ITrigger BuildCronTrigger(string cronStr)
        {
            return TriggerBuilder.Create()
                .WithIdentity(Guid.NewGuid().ToString("N"))
                .WithCronSchedule(cronStr)
                .Build();
        }

        private ITrigger BuildNeverGonnaFireTrigger()
        {
            return TriggerBuilder.Create()
                .WithIdentity(Guid.NewGuid().ToString("N"))
                .StartAt(DateBuilder.DateOf(1, 0, 0, 1, 1, 2099))
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(1) // doesn't matter - aint gunna fire
                    .RepeatForever())
                .Build();
        }
        private ITrigger BuildImmediateFireTrigger()
        {
            return TriggerBuilder.Create()
                .WithIdentity(Guid.NewGuid().ToString("N"))
                .StartAt(DateBuilder.DateOf(1, 0, 0, 1, 1, 2099)) // far far in the future
                .StartNow()
                .Build();
        }

        /// <summary>
        /// will run once immediately
        /// </summary>
        /// <returns></returns>
        private ITrigger BuildDebugTrigger()
        {
            return TriggerBuilder.Create()
                .WithIdentity(Guid.NewGuid().ToString("N"))
                .StartNow()
                .Build();
        }


        private void CheckBuilt()
        {
            if (!_isBuilt)
            {
                LogError("Not Built");
                throw new ConfigurationErrorsException($"{GetType().Name}: Not Built. Must call Build() before accessing trigger()");
            }
        }

        private string GetPrintDate(DateTime date)
        {
            if (date.Year > 2050)
                return "MANUALLY TRIGGERED";
            return $"{date.ToShortDateString()} {date.ToShortTimeString()}";
        }

        private string GetPrintDateFromUtc(DateTime date)
        {
            //var dt = TimeZoneInfo.ConvertTimeFromUtc(date, _timeZone);
            var dt = date;
            return GetPrintDate(dt);
        }

        private void Log(string msg)
        {
            _logger?.LogInformation($"{GetType().Name}: {msg}");
        }

        private void LogWarn(string msg)
        {
            _logger?.LogWarning($"{GetType().Name}: {msg}");
        }

        private void LogError(string msg)
        {
            _logger?.LogError($"{GetType().Name}: {msg}");
        }

    }
}
