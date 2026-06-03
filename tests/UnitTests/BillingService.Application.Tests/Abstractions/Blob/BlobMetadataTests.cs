using BillingService.Application.Abstractions.Blob;
using Xunit;

namespace BillingService.Application.Tests.Abstractions.Blob;

public sealed class BlobMetadataTests
{
    [Theory]
    [InlineData("", "rtafiles", "corr-1")]
    [InlineData(" ", "rtafiles", "corr-1")]
    public void Constructor_ThrowsArgumentException_WhenBlobNameIsNullOrWhitespace(
        string blobName, string container, string correlationId)
    {
        Assert.Throws<ArgumentException>(() => new BlobMetadata(blobName, container, correlationId));
    }

    [Theory]
    [InlineData("claims/batch.edi", "", "corr-1")]
    [InlineData("claims/batch.edi", " ", "corr-1")]
    public void Constructor_ThrowsArgumentException_WhenContainerNameIsNullOrWhitespace(
        string blobName, string container, string correlationId)
    {
        Assert.Throws<ArgumentException>(() => new BlobMetadata(blobName, container, correlationId));
    }

    [Theory]
    [InlineData("claims/batch.edi", "rtafiles", "")]
    [InlineData("claims/batch.edi", "rtafiles", " ")]
    public void Constructor_ThrowsArgumentException_WhenCorrelationIdIsNullOrWhitespace(
        string blobName, string container, string correlationId)
    {
        Assert.Throws<ArgumentException>(() => new BlobMetadata(blobName, container, correlationId));
    }

    [Fact]
    public void Constructor_AssignsProperties_WhenArgumentsAreValid()
    {
        var metadata = new BlobMetadata("claims/2024/batch.edi", "rtafiles", "corr-abc");

        Assert.Equal("claims/2024/batch.edi", metadata.BlobName);
        Assert.Equal("rtafiles", metadata.ContainerName);
        Assert.Equal("corr-abc", metadata.CorrelationId);
        Assert.Empty(metadata.Tags);
    }

    [Fact]
    public void Tags_CanBeSetViaInitializer()
    {
        var tags = new Dictionary<string, string> { ["claimId"] = "123" };
        var metadata = new BlobMetadata("b", "c", "corr") { Tags = tags };

        Assert.Equal("123", metadata.Tags["claimId"]);
    }
}
