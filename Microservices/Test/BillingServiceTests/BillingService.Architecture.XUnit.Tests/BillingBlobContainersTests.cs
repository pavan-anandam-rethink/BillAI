using BillingService.Application.Abstractions.BlobStorage;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class BillingBlobContainersTests
{
    [Theory]
    [InlineData(BillingBlobContainers.ClaimSubmissions)]
    [InlineData(BillingBlobContainers.EraResponses)]
    [InlineData(BillingBlobContainers.Acknowledgements)]
    [InlineData(BillingBlobContainers.EligibilityFiles)]
    [InlineData(BillingBlobContainers.PatientInvoices)]
    [InlineData(BillingBlobContainers.ClaimAttachments)]
    [InlineData(BillingBlobContainers.PaymentAttachments)]
    [InlineData(BillingBlobContainers.Reports)]
    [InlineData(BillingBlobContainers.AuditArchives)]
    public void ContainerName_StartsWithBillingPrefix(string containerName)
    {
        Assert.StartsWith("billing-", containerName);
    }

    [Theory]
    [InlineData(BillingBlobContainers.ClaimSubmissions)]
    [InlineData(BillingBlobContainers.EraResponses)]
    [InlineData(BillingBlobContainers.Acknowledgements)]
    [InlineData(BillingBlobContainers.EligibilityFiles)]
    [InlineData(BillingBlobContainers.PatientInvoices)]
    [InlineData(BillingBlobContainers.ClaimAttachments)]
    [InlineData(BillingBlobContainers.PaymentAttachments)]
    [InlineData(BillingBlobContainers.Reports)]
    [InlineData(BillingBlobContainers.AuditArchives)]
    public void ContainerName_IsLowercaseOnly(string containerName)
    {
        Assert.Equal(containerName.ToLowerInvariant(), containerName);
    }

    [Theory]
    [InlineData(BillingBlobContainers.ClaimSubmissions)]
    [InlineData(BillingBlobContainers.EraResponses)]
    [InlineData(BillingBlobContainers.Acknowledgements)]
    public void ContainerName_MaxLength_IsWithinAzureLimit(string containerName)
    {
        // Azure Blob Storage container name max length is 63 characters
        Assert.True(containerName.Length <= 63, $"Container name '{containerName}' exceeds 63 characters.");
    }
}
