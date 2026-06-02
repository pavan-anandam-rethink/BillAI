using BillingService.Domain.Models;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;

namespace BillingService.LegacyAdapters.PatientInvoice;

/// <summary>
/// Anti-corruption facade interface for patient invoice operations.
/// Mirrors the existing <c>IPatientInvoiceService</c> contract with a cancellation-token
/// aware signature so controllers and future CQRS handlers can depend on this stable seam.
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

    Task<List<PatientInvoiceViewModel>> GetPreviousInvoicesAsync(
        int accountId,
        int clientId,
        string invoiceNo,
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
