using BillingService.Domain.Models;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;

namespace BillingService.LegacyAdapters.PatientInvoice;

/// <summary>
/// Application-facing compatibility interface for patient invoice operations.
/// Provides a stable seam between CQRS handlers and the legacy
/// <see cref="BillingService.Domain.Interfaces.PatientInvoice.IPatientInvoiceService"/>.
/// </summary>
public interface IPatientInvoiceCompatibilityFacade
{
    Task<(IEnumerable<PatientInvoiceCreationModel> Data, int TotalCount)> GetPICreationDetailsAsync(
        CreateInvoiceFilters filters,
        CancellationToken cancellationToken = default);

    Task<(byte[] PdfData, List<string> ErrorList)> GeneratePdfAsync(
        List<InvoiceRequestModel> invoiceRequests,
        bool isSubmit,
        bool includePreviousInvoices,
        string invoiceNumber,
        CancellationToken cancellationToken = default);

    Task<PatientInvoiceViewModel> GenerateInvoiceAsync(
        int accountId,
        int clientId,
        List<ChargeModel> charges,
        CancellationToken cancellationToken = default);

    Task<(List<InvoiceDetailsModel> Data, List<ClaimFilterOptionModel> UserList, int TotalCount)> GetInvoiceDetailsAsync(
        PendingCollectionFilters filters,
        CancellationToken cancellationToken = default);

    Task<(byte[] PdfData, List<string> ErrorList)> GetInvoicePdfAsync(
        int accountId,
        int clientId,
        string invoiceNo,
        CancellationToken cancellationToken = default);
}
