using BillingService.App.Application.Abstractions;
using MediatR;

namespace BillingService.App.Application.Claims.Queries;

public sealed record GetClaimHeadersQuery(
    string JsonPayload,
    IReadOnlyDictionary<string, string> ForwardHeaders,
    bool UseCache) : IRequest<LegacyGatewayResponse>;

