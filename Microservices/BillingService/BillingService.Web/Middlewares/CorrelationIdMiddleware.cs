using BillingService.Web.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Web.Middlewares
{
    public sealed class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private readonly BillingModernizationOptions _options;

        public CorrelationIdMiddleware(
            RequestDelegate next,
            IOptions<BillingModernizationOptions> options,
            ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
            _options.Normalize();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_options.Correlation.Enabled)
            {
                await _next(context);
                return;
            }

            var headerName = _options.Correlation.HeaderName;
            var correlationId = GetOrCreateCorrelationId(context, headerName);
            context.TraceIdentifier = correlationId;
            context.Response.Headers[headerName] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                await _next(context);
            }
        }

        private static string GetOrCreateCorrelationId(HttpContext context, string headerName)
        {
            if (context.Request.Headers.TryGetValue(headerName, out var values))
            {
                var incomingValue = values.ToString();
                if (!string.IsNullOrWhiteSpace(incomingValue))
                {
                    return incomingValue;
                }
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
