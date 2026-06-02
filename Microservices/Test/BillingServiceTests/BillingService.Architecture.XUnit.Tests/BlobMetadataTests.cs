using BillingService.Application.Abstractions.Blob;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class BlobMetadataTests
{
    [Fact]
    public void BuildBlobName_ProducesStructuredPath()
    {
        var metadata = new BlobMetadata
        {
            AccountInfoId = 42,
            Area = "edi",
            OperationType = "claim-submission",
            CorrelationId = "abc123",
            OccurredOnUtc = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero)
        };

        var blobName = metadata.BuildBlobName("837p.edi");

        Assert.Equal("42/edi/2024/06/15/claim-submission/abc123/837p.edi", blobName);
    }

    [Fact]
    public void BuildBlobName_NormalizesAreaAndOperationType()
    {
        var metadata = new BlobMetadata
        {
            AccountInfoId = 1,
            Area = "EDI Files",
            OperationType = "ERA Response",
            CorrelationId = "x",
            OccurredOnUtc = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var blobName = metadata.BuildBlobName("file.txt");

        Assert.Contains("edi files", blobName);
        Assert.Contains("era response", blobName);
    }

    [Fact]
    public void BuildBlobName_ThrowsWhenFileNameIsEmpty()
    {
        var metadata = new BlobMetadata
        {
            AccountInfoId = 1,
            Area = "claims",
            CorrelationId = "c1"
        };

        Assert.Throws<ArgumentException>(() => metadata.BuildBlobName(string.Empty));
    }

    [Fact]
    public void BuildBlobName_UsesUnknownForMissingArea()
    {
        var metadata = new BlobMetadata
        {
            AccountInfoId = 5,
            CorrelationId = "c2"
        };

        var name = metadata.BuildBlobName("test.json");

        Assert.Contains("unknown", name);
    }

    [Fact]
    public void Metadata_DefaultsAreNotNull()
    {
        var metadata = new BlobMetadata();

        Assert.NotNull(metadata.Tags);
        Assert.Equal("application/octet-stream", metadata.ContentType);
    }
}
