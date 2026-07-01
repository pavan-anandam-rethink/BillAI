using ClaimLifecycleMonitoring.Application.Common.Exceptions;
using ClaimLifecycleMonitoring.Contracts.Common;
using ClaimLifecycleMonitoring.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

namespace ClaimLifecycleMonitoring.API.Middleware;

/// <summary>
/// Global exception handling middleware that translates known application / domain
/// exceptions into the appropriate HTTP responses with a uniform error envelope.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware
{
    /// <summary>Correlation header name.</summary>
    public const string CorrelationHeader = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    /// <summary>Creates a new <see cref="GlobalExceptionHandlingMiddleware"/>.</summary>
    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Invokes the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ApplicationValidationException vex)
        {
            _logger.LogWarning(vex, "Validation failure on {Path}", context.Request.Path);
            await WriteAsync(context, HttpStatusCode.BadRequest, "validation_failed", vex.Message,
                vex.Errors.SelectMany(kv => kv.Value.Select(m => new ValidationFailureDto { Property = kv.Key, Message = m })).ToList())
                .ConfigureAwait(false);
        }
        catch (DomainValidationException dex)
        {
            _logger.LogWarning(dex, "Domain validation failure on {Path}", context.Request.Path);
            await WriteAsync(context, HttpStatusCode.BadRequest, "domain_validation_failed", dex.Message, Array.Empty<ValidationFailureDto>())
                .ConfigureAwait(false);
        }
        catch (EntityNotFoundException nex)
        {
            _logger.LogInformation(nex, "Entity not found on {Path}", context.Request.Path);
            await WriteAsync(context, HttpStatusCode.NotFound, "not_found", nex.Message, Array.Empty<ValidationFailureDto>())
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected; nothing more to do.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await WriteAsync(context, HttpStatusCode.InternalServerError, "server_error", "An unexpected error occurred.", Array.Empty<ValidationFailureDto>())
                .ConfigureAwait(false);
        }
    }

    private static async Task WriteAsync(HttpContext context, HttpStatusCode status, string code, string message, IReadOnlyList<ValidationFailureDto> failures)
    {
        if (context.Response.HasStarted)
        {
            return;
        }
        context.Response.Clear();
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        var correlationId = context.Response.Headers[CorrelationHeader].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = context.TraceIdentifier;
        }

        var payload = new ApiErrorResponse
        {
            Code = code,
            Message = message,
            CorrelationId = correlationId,
            Failures = failures
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json).ConfigureAwait(false);
    }
}
