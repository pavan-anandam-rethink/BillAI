using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;

namespace BillingService.LegacyAdapters.PatientInvoice;

/// <summary>
/// Delegates every call to the underlying <see cref="IPatientInvoiceService"/>.
/// Preserves 100% of the existing domain behavior (PDF generation, invoice aggregation,
/// charge grouping) while exposing a stable, testable seam.
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

    public Task<List<PatientInvoiceViewModel>> GetPreviousInvoicesAsync(
        int accountId,
        int clientId,
        string invoiceNo,
        CancellationToken cancellationToken = default)
        => patientInvoiceService.GetPreviousInvoices(accountId, clientId, invoiceNo);

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
