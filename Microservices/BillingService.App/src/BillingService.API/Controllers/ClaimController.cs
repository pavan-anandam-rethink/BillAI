using System.Text.Json;
using BillingService.App.Application.Abstractions;
using BillingService.App.Application.Claims.Queries;
using BillingService.App.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillingService.App.API.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]")]
public sealed class ClaimController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILegacyBillingGateway _legacyBillingGateway;
    private readonly BillingModernizationSettings _settings;
    private readonly ILogger<ClaimController> _logger;

    public ClaimController(
        IMediator mediator,
        ILegacyBillingGateway legacyBillingGateway,
        IOptions<BillingModernizationSettings> settings,
        ILogger<ClaimController> logger)
    {
        _mediator = mediator;
        _legacyBillingGateway = legacyBillingGateway;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> GetClaimHeaders([FromBody] JsonElement request, CancellationToken cancellationToken)
    {
        var payload = request.GetRawText();
        var headers = GetForwardHeaders();

        if (_settings.ClaimHeadersMigrationMode == MigrationMode.Off)
        {
            var legacyResponse = await _legacyBillingGateway
                .ForwardJsonAsync("Claim/GetClaimHeaders", payload, headers, cancellationToken)
                .ConfigureAwait(false);

            return BuildContentResult(legacyResponse);
        }

        var cqrsResponse = await _mediator
            .Send(new GetClaimHeadersQuery(payload, headers, useCache: true), cancellationToken)
            .ConfigureAwait(false);

        if (_settings.ClaimHeadersMigrationMode == MigrationMode.Shadow)
        {
            var legacyResponse = await _legacyBillingGateway
                .ForwardJsonAsync("Claim/GetClaimHeaders", payload, headers, cancellationToken)
                .ConfigureAwait(false);

            if (!string.Equals(cqrsResponse.Content, legacyResponse.Content, StringComparison.Ordinal))
            {
                _logger.LogWarning("Shadow mode mismatch detected for Claim/GetClaimHeaders");
            }

            return BuildContentResult(legacyResponse);
        }

        return BuildContentResult(cqrsResponse);
    }

    private Dictionary<string, string> GetForwardHeaders()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            result["Authorization"] = auth.ToString();
        }

        if (Request.Headers.TryGetValue("XApiKey", out var apiKey))
        {
            result["XApiKey"] = apiKey.ToString();
        }

        if (Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
        {
            result["X-Correlation-Id"] = correlationId.ToString();
        }

        return result;
    }

    private static ContentResult BuildContentResult(LegacyGatewayResponse response)
    {
        return new ContentResult
        {
            StatusCode = response.StatusCode,
            Content = response.Content,
            ContentType = response.ContentType
        };
    }
}

