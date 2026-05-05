using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Jobs
{
    [ExcludeFromCodeCoverage]
    public static class DefaultTaskFactory
    {
        private static TaskFactory _taskFactory = null;
        private static readonly object _lock = new object();

        public static TaskFactory Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_taskFactory == null)
                    {
                        _taskFactory = new TaskFactory(
                            new LimitedConcurrencyLevelTaskScheduler(
                                MaxDegreeOfParallelism));
                    }

                    return _taskFactory;
                }
            }
        }

        public static int MaxDegreeOfParallelism
        {
            get { return Math.Max(Environment.ProcessorCount, 4); }
        }
    }
}
