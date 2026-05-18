namespace BillingService.App.Application.Abstractions;

public sealed record LegacyGatewayResponse(int StatusCode, string Content, string ContentType);

public interface ILegacyBillingGateway
{
    Task<LegacyGatewayResponse> ForwardJsonAsync(
        string relativePath,
        string jsonPayload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken);
}

