using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;

namespace BillingService.LegacyAdapters.PatientInvoice;

/// <summary>
/// Wraps <see cref="IPatientInvoiceService"/> as a compatibility facade.
/// All existing invoice generation and retrieval behavior is preserved by delegation —
/// no business logic is added or removed here.
/// </summary>
public sealed class PatientInvoiceCompatibilityFacade(IPatientInvoiceService patientInvoiceService)
    : IPatientInvoiceCompatibilityFacade
{
    public Task<(IEnumerable<PatientInvoiceCreationModel> Data, int TotalCount)> GetPICreationDetailsAsync(
        CreateInvoiceFilters filters,
        CancellationToken cancellationToken = default)
        => patientInvoiceService.GetPICreationDetails(filters);

    public Task<(byte[] PdfData, List<string> ErrorList)> GeneratePdfAsync(
        List<InvoiceRequestModel> invoiceRequests,
        bool isSubmit,
        bool includePreviousInvoices,
        string invoiceNumber,
        CancellationToken cancellationToken = default)
        => patientInvoiceService.GeneratePDF(invoiceRequests, isSubmit, includePreviousInvoices, invoiceNumber);

    public Task<PatientInvoiceViewModel> GenerateInvoiceAsync(
        int accountId,
        int clientId,
        List<ChargeModel> charges,
        CancellationToken cancellationToken = default)
        => patientInvoiceService.GenerateInvoice(accountId, clientId, charges);

    public Task<(List<InvoiceDetailsModel> Data, List<ClaimFilterOptionModel> UserList, int TotalCount)> GetInvoiceDetailsAsync(
        PendingCollectionFilters filters,
        CancellationToken cancellationToken = default)
        => patientInvoiceService.GetInvoiceDetails(filters);

    public Task<(byte[] PdfData, List<string> ErrorList)> GetInvoicePdfAsync(
        int accountId,
        int clientId,
        string invoiceNo,
        CancellationToken cancellationToken = default)
        => patientInvoiceService.GetInvoicePDF(accountId, clientId, invoiceNo);
}
