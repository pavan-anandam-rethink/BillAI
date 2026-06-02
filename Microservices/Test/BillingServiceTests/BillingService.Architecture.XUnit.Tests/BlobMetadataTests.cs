using BillingService.Application.Abstractions.Blob;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class BlobMetadataTests
{
    [Fact]
    public void BlobMetadata_DoesNotExposeFilePath()
    {
        // Verify the contract: BlobMetadata must never carry a file path or storage URL.
        var properties = typeof(BlobMetadata).GetProperties();

        foreach (var property in properties)
        {
            var name = property.Name;
            Assert.False(
                name.Contains("Path", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Url", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Uri", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("FilePath", StringComparison.OrdinalIgnoreCase),
                $"BlobMetadata must not contain a property named '{name}'. " +
                "Blob-first architecture forbids storing file paths or blob URLs in the database.");
        }
    }

    [Fact]
    public void BlobMetadata_RequiresBlobNameAndContainer()
    {
        // BlobName and ContainerName are required to reconstruct the blob address at retrieval.
        var blobNameProperty = typeof(BlobMetadata).GetProperty(nameof(BlobMetadata.BlobName));
        var containerProperty = typeof(BlobMetadata).GetProperty(nameof(BlobMetadata.ContainerName));

        Assert.NotNull(blobNameProperty);
        Assert.NotNull(containerProperty);
        Assert.Equal(typeof(string), blobNameProperty!.PropertyType);
        Assert.Equal(typeof(string), containerProperty!.PropertyType);
    }

    [Fact]
    public void BlobMetadata_HasCorrelationId()
    {
        // Every blob artifact must be traceable via correlation ID.
        var property = typeof(BlobMetadata).GetProperty(nameof(BlobMetadata.CorrelationId));

        Assert.NotNull(property);
        Assert.Equal(typeof(string), property!.PropertyType);
    }

    [Fact]
    public void BlobMetadata_HasDefaultUploadedStatus()
    {
        var metadata = new BlobMetadata
        {
            BlobName = "edi/2024/06/02/test/837-claim.x12",
            ContainerName = "rtafiles",
            Category = "edi-837",
            CorrelationId = "abc123"
        };

        Assert.Equal(BlobProcessingStatus.Uploaded, metadata.Status);
    }

    [Fact]
    public void BlobStorageOptions_HasWellKnownContainerNames()
    {
        var options = new BlobStorageOptions();

        Assert.False(string.IsNullOrWhiteSpace(options.EdiContainerName),
            "EdiContainerName must have a default value.");
        Assert.False(string.IsNullOrWhiteSpace(options.EraContainerName),
            "EraContainerName must have a default value.");
        Assert.False(string.IsNullOrWhiteSpace(options.AvailityContainerName),
            "AvailityContainerName must have a default value.");
        Assert.False(string.IsNullOrWhiteSpace(options.InvoicePdfContainerName),
            "InvoicePdfContainerName must have a default value.");
    }

    [Fact]
    public void IBlobMetadataStore_DefinesRequiredOperations()
    {
        var methods = typeof(IBlobMetadataStore).GetMethods();
        var methodNames = methods.Select(m => m.Name).ToHashSet();

        Assert.Contains(nameof(IBlobMetadataStore.RecordAsync), methodNames);
        Assert.Contains(nameof(IBlobMetadataStore.MarkProcessedAsync), methodNames);
        Assert.Contains(nameof(IBlobMetadataStore.MarkFailedAsync), methodNames);
        Assert.Contains(nameof(IBlobMetadataStore.GetByCorrelationIdAsync), methodNames);
        Assert.Contains(nameof(IBlobMetadataStore.GetByAccountAndCategoryAsync), methodNames);
    }
}
