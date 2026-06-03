using BillingService.Application.Abstractions.Blob;
using BillingService.Application.Abstractions.Correlation;
using BillingService.Application.Abstractions.Messaging;
using BillingService.Application.Common.Configuration;
using BillingService.Contracts.Metadata;

namespace BillingService.Architecture.XUnit.Tests;

/// <summary>
/// Modernization compatibility tests.
/// Verify that new architectural seams (blob, correlation ID, outbox) expose the
/// expected abstractions and that no file paths or blob URLs leak into BlobMetadata.
/// These tests guard the zero-regression contract during incremental migration.
/// </summary>
public sealed class ModernizationCompatibilityTests
{
    // ── BlobMetadata ─────────────────────────────────────────────────────────

    [Fact]
    public void BlobMetadata_DoesNotContainFilePathProperty()
    {
        var properties = typeof(BlobMetadata).GetProperties();
        var names = properties.Select(p => p.Name).ToArray();

        Assert.DoesNotContain("FilePath", names);
        Assert.DoesNotContain("BlobUrl", names);
        Assert.DoesNotContain("Uri", names);
        Assert.DoesNotContain("Url", names);
        Assert.DoesNotContain("Path", names);
    }

    [Fact]
    public void BlobMetadata_ContainsRequiredBlobFirstProperties()
    {
        var properties = typeof(BlobMetadata).GetProperties()
            .Select(p => p.Name)
            .ToArray();

        Assert.Contains("BlobName", properties);
        Assert.Contains("ContainerName", properties);
        Assert.Contains("CorrelationId", properties);
        Assert.Contains("Tags", properties);
    }

    [Fact]
    public void BlobMetadata_CorrelationIdIsNullable()
    {
        var prop = typeof(BlobMetadata).GetProperty(nameof(BlobMetadata.CorrelationId))!;
        var nullabilityCtx = new System.Reflection.NullabilityInfoContext();
        var info = nullabilityCtx.Create(prop);

        Assert.Equal(System.Reflection.NullabilityState.Nullable, info.WriteState);
    }

    [Fact]
    public void BlobMetadata_IsRecord()
    {
        Assert.True(typeof(BlobMetadata).IsClass);

        // Records expose EqualityContract; value equality is a record contract.
        var a = new BlobMetadata { BlobName = "test.edi", ContainerName = "billing-artifacts", CorrelationId = "abc" };
        var b = new BlobMetadata { BlobName = "test.edi", ContainerName = "billing-artifacts", CorrelationId = "abc" };

        Assert.Equal(a, b);
    }

    // ── IBlobStorageService ───────────────────────────────────────────────────

    [Fact]
    public void IBlobStorageService_ExposesRequiredMethods()
    {
        var methods = typeof(IBlobStorageService).GetMethods()
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains("UploadAsync", methods);
        Assert.Contains("DownloadAsync", methods);
        Assert.Contains("ExistsAsync", methods);
        Assert.Contains("DeleteAsync", methods);
        Assert.Contains("ListAsync", methods);
        Assert.Contains("EnsureContainerExistsAsync", methods);
    }

    // ── IBlobMetadataStore ────────────────────────────────────────────────────

    [Fact]
    public void IBlobMetadataStore_ExposesRequiredMethods()
    {
        var methods = typeof(IBlobMetadataStore).GetMethods()
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains("SaveAsync", methods);
        Assert.Contains("GetByCorrelationIdAsync", methods);
        Assert.Contains("ListByContainerAsync", methods);
    }

    // ── ICorrelationIdProvider ────────────────────────────────────────────────

    [Fact]
    public void ICorrelationIdProvider_ExposesGetCorrelationId()
    {
        var method = typeof(ICorrelationIdProvider).GetMethod(nameof(ICorrelationIdProvider.GetCorrelationId));
        Assert.NotNull(method);
        Assert.Equal(typeof(string), method!.ReturnType);
    }

    // ── IOutboxPoller ─────────────────────────────────────────────────────────

    [Fact]
    public void IOutboxPoller_ExposesRequiredMethods()
    {
        var methods = typeof(IOutboxPoller).GetMethods()
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains("FetchPendingAsync", methods);
        Assert.Contains("MarkProcessedAsync", methods);
        Assert.Contains("RecordFailureAsync", methods);
    }

    // ── ModernizationFeatureFlags ─────────────────────────────────────────────

    [Fact]
    public void ModernizationFeatureFlags_ContainsBlobFirstStorageFlag()
    {
        var prop = typeof(ModernizationFeatureFlags)
            .GetProperty(nameof(ModernizationFeatureFlags.EnableBlobFirstStorage));

        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop!.PropertyType);
    }

    [Fact]
    public void ModernizationFeatureFlags_SectionNameMatchesConfigKey()
    {
        Assert.Equal("BillingService:Modernization", ModernizationFeatureFlags.SectionName);
    }

    // ── API contract inventory ────────────────────────────────────────────────

    [Fact]
    public void BillingApiContractInventory_HealthRouteIsCorrect()
    {
        Assert.Equal("/api/health", BillingApiContractInventory.HealthRoute);
    }

    [Fact]
    public void BillingApiContractInventory_PusherAuthRouteIsCorrect()
    {
        Assert.Equal("pusher/auth", BillingApiContractInventory.PusherAuthRoute);
    }

    [Fact]
    public void BillingApiContractInventory_CompatibilityCriticalControllers_NotEmpty()
    {
        Assert.NotEmpty(BillingApiContractInventory.CompatibilityCriticalControllers);
    }
}
