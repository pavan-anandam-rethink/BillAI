using ClearingHouse.SharedKernel.Domain;

namespace StediIntegration.Domain.ValueObjects;

public class StediApiCredentials : ValueObject
{
    public string ApiKey { get; }
    public string BaseUrl { get; }
    public string PartnerId { get; }

    public StediApiCredentials(string apiKey, string baseUrl, string partnerId)
    {
        ApiKey = apiKey;
        BaseUrl = baseUrl;
        PartnerId = partnerId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ApiKey;
        yield return BaseUrl;
        yield return PartnerId;
    }
}
