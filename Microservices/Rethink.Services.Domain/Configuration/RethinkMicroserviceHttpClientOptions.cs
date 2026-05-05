using Microsoft.Extensions.Configuration;
using System;

namespace Rethink.Services.Domain.Configuration
{
    internal static class RethinkMicroserviceHttpClientOptions
    {
        public static TimeSpan GetRequestTimeout(IConfiguration configuration)
        {
            var seconds = 120;
            if (int.TryParse(configuration["BillingHttp:RequestTimeoutSeconds"], out var parsed))
            {
                seconds = parsed;
            }

            if (seconds < 10) seconds = 10;
            if (seconds > 600) seconds = 600;
            return TimeSpan.FromSeconds(seconds);
        }

        public static bool UseResilience(IConfiguration configuration)
        {
            var v = configuration["BillingHttp:ResilienceEnabled"];
            if (string.IsNullOrEmpty(v)) return true;
            return bool.TryParse(v, out var enabled) && enabled;
        }
    }
}
