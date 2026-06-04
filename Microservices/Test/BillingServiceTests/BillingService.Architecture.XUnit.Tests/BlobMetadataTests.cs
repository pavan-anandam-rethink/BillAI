using BillingService.Application.Abstractions.Blob;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class BlobMetadataTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var metadata = new BlobMetadata("edi/2026/06/04/abc123.x12", "billing-edi", "corr-001");

        Assert.Equal("edi/2026/06/04/abc123.x12", metadata.BlobName);
        Assert.Equal("billing-edi", metadata.ContainerName);
        Assert.Equal("corr-001", metadata.CorrelationId);
    }

    [Fact]
    public void Constructor_AcceptsNullCorrelationId()
    {
        var metadata = new BlobMetadata("report.pdf", "billing-invoices");

        Assert.Equal("report.pdf", metadata.BlobName);
        Assert.Equal("billing-invoices", metadata.ContainerName);
        Assert.Null(metadata.CorrelationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlanKBlobName(string blobName)
    {
        Assert.Throws<ArgumentException>(() => new BlobMetadata(blobName, "billing-edi"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankContainerName(string containerName)
    {
        Assert.Throws<ArgumentException>(() => new BlobMetadata("valid-blob.x12", containerName));
    }

    [Fact]
    public void BlobMetadata_DoesNotExposeFilePathsOrUrls()
    {
        // BlobMetadata MUST NOT contain properties that store file paths or blob URLs.
        var metadataType = typeof(BlobMetadata);

        var forbiddenNames = new[] { "FilePath", "Path", "Url", "Uri", "AbsolutePath", "PhysicalPath" };

        foreach (var name in forbiddenNames)
        {
            Assert.True(
                metadataType.GetProperty(name) is null,
                $"BlobMetadata must not expose a '{name}' property. " +
                "Blob-first architecture requires BlobName+ContainerName+CorrelationId only.");
        }
    }

    [Fact]
    public void BlobStorageOptions_SectionNameIsCorrect()
    {
        Assert.Equal("BillingService:BlobStorage", BlobStorageOptions.SectionName);
    }

    [Fact]
    public void BlobStorageOptions_DefaultContainersAreNonEmpty()
    {
        var options = new BlobStorageOptions();

        Assert.False(string.IsNullOrWhiteSpace(options.DefaultContainer));
        Assert.False(string.IsNullOrWhiteSpace(options.EdiContainer));
        Assert.False(string.IsNullOrWhiteSpace(options.InvoiceContainer));
        Assert.False(string.IsNullOrWhiteSpace(options.EraContainer));
    }
}
