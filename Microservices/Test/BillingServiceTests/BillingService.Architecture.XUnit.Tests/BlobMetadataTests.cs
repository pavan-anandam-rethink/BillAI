using BillingService.Application.Abstractions.BlobStorage;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class BlobMetadataTests
{
    [Fact]
    public void BlobMetadata_DefaultContentType_IsOctetStream()
    {
        var metadata = new BlobMetadata();

        Assert.Equal("application/octet-stream", metadata.ContentType);
    }

    [Fact]
    public void BlobMetadata_Tags_DefaultsToEmpty()
    {
        var metadata = new BlobMetadata();

        Assert.Empty(metadata.Tags);
    }

    [Fact]
    public void BlobMetadata_Init_SetsAllProperties()
    {
        var tags = new Dictionary<string, string> { ["env"] = "test" };
        var metadata = new BlobMetadata
        {
            CorrelationId = "corr-123",
            TenantId = "42",
            Domain = "claims",
            DocumentType = "837",
            ContentType = "application/json",
            Tags = tags
        };

        Assert.Equal("corr-123", metadata.CorrelationId);
        Assert.Equal("42", metadata.TenantId);
        Assert.Equal("claims", metadata.Domain);
        Assert.Equal("837", metadata.DocumentType);
        Assert.Equal("application/json", metadata.ContentType);
        Assert.Single(metadata.Tags);
        Assert.Equal("test", metadata.Tags["env"]);
    }
}
