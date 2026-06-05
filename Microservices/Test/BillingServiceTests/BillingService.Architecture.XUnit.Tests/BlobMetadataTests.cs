using BillingService.Application.Abstractions.Blob;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class BlobMetadataTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var metadata = new BlobMetadata("claims/837/corr-001.edi", "billingfiles", "corr-001");

        Assert.Equal("claims/837/corr-001.edi", metadata.BlobName);
        Assert.Equal("billingfiles", metadata.ContainerName);
        Assert.Equal("corr-001", metadata.CorrelationId);
    }

    [Theory]
    [InlineData("", "container", "corr")]
    [InlineData("  ", "container", "corr")]
    [InlineData("blob", "", "corr")]
    [InlineData("blob", "  ", "corr")]
    [InlineData("blob", "container", "")]
    [InlineData("blob", "container", "  ")]
    public void Constructor_RejectsBlankArguments(string blobName, string containerName, string correlationId)
    {
        Assert.Throws<ArgumentException>(() => new BlobMetadata(blobName, containerName, correlationId));
    }

    [Fact]
    public void BlobMetadata_DoesNotExposeFilePathOrUrl()
    {
        var metadata = new BlobMetadata("claims/837/file.edi", "rtafiles", "corr-abc");

        var properties = typeof(BlobMetadata).GetProperties();
        var propertyNames = properties.Select(p => p.Name).ToArray();

        Assert.DoesNotContain("FilePath", propertyNames);
        Assert.DoesNotContain("Url", propertyNames);
        Assert.DoesNotContain("Uri", propertyNames);
        Assert.DoesNotContain("StoragePath", propertyNames);
    }

    [Fact]
    public void Tags_DefaultsToEmptyDictionary()
    {
        var metadata = new BlobMetadata("blob", "container", "corr");

        Assert.NotNull(metadata.Tags);
        Assert.Empty(metadata.Tags);
    }

    [Fact]
    public void Tags_CanBeSetOnInit()
    {
        var metadata = new BlobMetadata("blob", "container", "corr")
        {
            Tags = new Dictionary<string, string>
            {
                ["claimId"] = "CLM-001",
                ["submissionType"] = "837P"
            }
        };

        Assert.Equal("CLM-001", metadata.Tags["claimId"]);
        Assert.Equal("837P", metadata.Tags["submissionType"]);
    }
}
