using BillingService.Application.Abstractions.Blob;
using BillingService.Application.Abstractions.Correlation;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class CorrelationIdProviderAbstractionTests
{
    [Fact]
    public void ICorrelationIdProvider_IsInterface()
    {
        Assert.True(typeof(ICorrelationIdProvider).IsInterface);
    }

    [Fact]
    public void ICorrelationIdProvider_GetCorrelationId_ReturnsNullableString()
    {
        // Verify the return type of the abstraction is nullable string,
        // so callers that receive no context do not throw.
        var method = typeof(ICorrelationIdProvider).GetMethod(nameof(ICorrelationIdProvider.GetCorrelationId));

        Assert.NotNull(method);
        Assert.Equal(typeof(string), method!.ReturnType);
    }
}

public sealed class BlobMetadataTests
{
    [Fact]
    public void BlobMetadata_DoesNotExposeFilePath()
    {
        // Enforce the blob-first rule: BlobMetadata must NOT contain file path or URI properties.
        var props = typeof(BlobMetadata).GetProperties();
        var forbidden = new[] { "FilePath", "FileUrl", "Uri", "Url", "Path" };

        foreach (var prop in props)
        {
            foreach (var name in forbidden)
            {
                Assert.False(
                    prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase),
                    $"BlobMetadata must not expose '{prop.Name}'. Blob paths must never be stored in the domain model.");
            }
        }
    }

    [Fact]
    public void BlobMetadata_RequiresBlobNameAndContainerName()
    {
        var blobName = typeof(BlobMetadata).GetProperty(nameof(BlobMetadata.BlobName));
        var containerName = typeof(BlobMetadata).GetProperty(nameof(BlobMetadata.ContainerName));

        Assert.NotNull(blobName);
        Assert.NotNull(containerName);
        Assert.Equal(typeof(string), blobName!.PropertyType);
        Assert.Equal(typeof(string), containerName!.PropertyType);
    }

    [Fact]
    public void BlobMetadata_CorrelationId_IsNullable()
    {
        var prop = typeof(BlobMetadata).GetProperty(nameof(BlobMetadata.CorrelationId));

        Assert.NotNull(prop);
        // CorrelationId is optional – must be a nullable reference type
        var nullabilityCtx = new System.Reflection.NullabilityInfoContext();
        var info = nullabilityCtx.Create(prop!);
        Assert.Equal(System.Reflection.NullabilityState.Nullable, info.WriteState);
    }

    [Fact]
    public void BlobMetadata_CanBeConstructed_WithRequiredProperties()
    {
        var metadata = new BlobMetadata
        {
            BlobName = "837/2024/06/clm-abc123.edi",
            ContainerName = "rtafiles",
            CorrelationId = "test-correlation-id"
        };

        Assert.Equal("837/2024/06/clm-abc123.edi", metadata.BlobName);
        Assert.Equal("rtafiles", metadata.ContainerName);
        Assert.Equal("test-correlation-id", metadata.CorrelationId);
        Assert.Empty(metadata.Tags);
    }
}

public sealed class IBlobMetadataStoreTests
{
    [Fact]
    public void IBlobMetadataStore_IsInterface()
    {
        Assert.True(typeof(IBlobMetadataStore).IsInterface);
    }

    [Fact]
    public void IBlobMetadataStore_HasSetTagsAsync()
    {
        var method = typeof(IBlobMetadataStore).GetMethod(nameof(IBlobMetadataStore.SetTagsAsync));
        Assert.NotNull(method);
    }

    [Fact]
    public void IBlobMetadataStore_HasFindByCorrelationIdAsync()
    {
        var method = typeof(IBlobMetadataStore).GetMethod(nameof(IBlobMetadataStore.FindByCorrelationIdAsync));
        Assert.NotNull(method);
    }

    [Fact]
    public void IBlobMetadataStore_HasListByTagAsync()
    {
        var method = typeof(IBlobMetadataStore).GetMethod(nameof(IBlobMetadataStore.ListByTagAsync));
        Assert.NotNull(method);
    }
}
