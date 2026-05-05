using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class LogConfigHelper
    {
        public static void ConfigureApiLogging(ILoggingBuilder logBuilder)
        {
            AddStandardConsole(logBuilder);
            AddStandardDebug(logBuilder);
        }
        public static void ConfigureWorkerLogging(ILoggingBuilder logBuilder)
        {
            AddStandardConsole(logBuilder);

            AddStandardDebug(logBuilder);
        }

        private static void AddStandardConsole(ILoggingBuilder logBuilder)
        {
            logBuilder.AddConsole(c =>
            {
                c.TimestampFormat = "[yyyy.MM.dd-HH:mm:ss.F] ";
            });

        }
        private static void AddStandardDebug(ILoggingBuilder logBuilder)
        {
#if DEBUG
            logBuilder.AddDebug()
                      .SetMinimumLevel(LogLevel.Debug);
#endif
        }
    }
}
