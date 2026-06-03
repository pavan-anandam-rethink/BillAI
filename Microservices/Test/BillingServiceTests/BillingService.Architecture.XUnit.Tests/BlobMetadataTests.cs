using BillingService.Application.Abstractions.Blob;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class BlobMetadataTests
{
    [Fact]
    public void BlobMetadata_RequiresBlobNameAndContainerName()
    {
        var metadata = new BlobMetadata
        {
            BlobName = "edi/837/2024/01/claim-abc.edi",
            ContainerName = "edi-requests"
        };

        Assert.Equal("edi/837/2024/01/claim-abc.edi", metadata.BlobName);
        Assert.Equal("edi-requests", metadata.ContainerName);
    }

    [Fact]
    public void BlobMetadata_CorrelationId_IsOptional()
    {
        var metadata = new BlobMetadata
        {
            BlobName = "reports/invoice-001.pdf",
            ContainerName = "rtafiles",
            CorrelationId = "abc-123"
        };

        Assert.Equal("abc-123", metadata.CorrelationId);
    }

    [Fact]
    public void BlobMetadata_DefaultTags_IsEmptyDictionary()
    {
        var metadata = new BlobMetadata
        {
            BlobName = "any-blob",
            ContainerName = "any-container"
        };

        Assert.NotNull(metadata.Tags);
        Assert.Empty(metadata.Tags);
    }

    [Fact]
    public void BlobMetadata_DoesNotExposeFilePathProperties()
    {
        // Blob-First rule: BlobMetadata must never carry file paths, URIs, or blob URL strings.
        // Validate that the type does not declare any such property.
        var type = typeof(BlobMetadata);
        var propertyNames = type.GetProperties().Select(p => p.Name).ToArray();

        var forbiddenSuffixes = new[] { "Path", "Uri", "Url", "Location", "FilePath" };

        foreach (var property in propertyNames)
        {
            foreach (var forbidden in forbiddenSuffixes)
            {
                Assert.False(
                    property.EndsWith(forbidden, StringComparison.OrdinalIgnoreCase),
                    $"BlobMetadata property '{property}' violates the Blob-First rule: " +
                    $"no file paths, URIs, or URL references are allowed.");
            }
        }
    }

    [Fact]
    public void BlobStorageOptions_DefaultContainerNames_AreNonEmpty()
    {
        var options = new BlobStorageOptions();

        Assert.False(string.IsNullOrWhiteSpace(options.DefaultContainerName));
        Assert.False(string.IsNullOrWhiteSpace(options.EdiRequestContainerName));
        Assert.False(string.IsNullOrWhiteSpace(options.EdiResponseContainerName));
        Assert.False(string.IsNullOrWhiteSpace(options.CorrelationIdTagName));
    }

    [Fact]
    public void BlobStorageOptions_MaxTagQueryResults_IsPositive()
    {
        var options = new BlobStorageOptions();

        Assert.True(options.MaxTagQueryResults > 0);
    }
}
