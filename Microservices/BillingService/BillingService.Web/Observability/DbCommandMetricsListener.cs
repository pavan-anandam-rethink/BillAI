using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BillingService.Web.Observability;

internal sealed class DbCommandMetricsListener : EventListener, IHostedService
{
    private const string EfCommandEventSource = "Microsoft.EntityFrameworkCore";
    private readonly ILogger<DbCommandMetricsListener> _logger;
    private volatile bool _isEnabled;

    public DbCommandMetricsListener(ILogger<DbCommandMetricsListener> logger)
    {
        _logger = logger;
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (!string.Equals(eventSource.Name, EfCommandEventSource, StringComparison.Ordinal))
        {
            return;
        }

        EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All);
        _isEnabled = true;
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (!_isEnabled || eventData.Payload == null || eventData.Payload.Count == 0)
        {
            return;
        }

        if (!string.Equals(eventData.EventName, "CommandExecuted", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            var payloadNames = eventData.PayloadNames ?? Array.Empty<string>();
            var payload = eventData.Payload;

            var duration = TryReadDouble(payloadNames, payload, "duration");
            if (!duration.HasValue)
            {
                return;
            }

            var commandText = TryReadString(payloadNames, payload, "commandText");
            var operation = ResolveOperation(commandText);
            ConcurrencyMetrics.RecordDbCommand(operation, duration.Value);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to process EF Core command event payload.");
        }
    }

    private static double? TryReadDouble(IReadOnlyList<string> names, IReadOnlyList<object> values, string key)
    {
        var index = IndexOf(names, key);
        if (index < 0 || index >= values.Count || values[index] == null)
        {
            return null;
        }

        if (values[index] is double d)
        {
            return d;
        }

        if (double.TryParse(values[index].ToString(), out d))
        {
            return d;
        }

        return null;
    }

    private static string TryReadString(IReadOnlyList<string> names, IReadOnlyList<object> values, string key)
    {
        var index = IndexOf(names, key);
        if (index < 0 || index >= values.Count || values[index] == null)
        {
            return string.Empty;
        }

        return values[index].ToString() ?? string.Empty;
    }

    private static string ResolveOperation(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return "unknown";
        }

        var firstToken = commandText.TrimStart()
            .Split(' ', '\r', '\n', '\t', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(firstToken)
            ? "unknown"
            : firstToken.ToUpperInvariant();
    }

    private static int IndexOf(IReadOnlyList<string> names, string key)
    {
        for (var i = 0; i < names.Count; i++)
        {
            if (string.Equals(names[i], key, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
