using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BillingService.LegacyAdapters.PatientInvoice;

/// <summary>
/// Wraps <see cref="IPatientInvoiceService"/> for injection into CQRS handlers and
/// future Application-layer extraction. All calls delegate unchanged to the legacy
/// domain service to ensure zero functional regression.
/// </summary>
public sealed class PatientInvoiceCompatibilityFacade(IPatientInvoiceService patientInvoiceService)
    : IPatientInvoiceCompatibilityFacade
{
    public Task<(IEnumerable<PatientInvoiceCreationModel> Data, int TotalCount)> GetPICreationDetailsAsync(
        CreateInvoiceFilters filters,
        CancellationToken cancellationToken = default)
    {
        return patientInvoiceService.GetPICreationDetails(filters);
    }

    public Task<(byte[] PdfData, List<string> ErrorList)> GeneratePdfAsync(
        List<InvoiceRequestModel> invoiceRequests,
        bool isSubmit,
        bool includePreviousInvoices,
        string invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        return patientInvoiceService.GeneratePDF(invoiceRequests, isSubmit, includePreviousInvoices, invoiceNumber);
    }

    public Task<PatientInvoiceViewModel> GenerateInvoiceAsync(
        int accountId,
        int clientId,
        List<ChargeModel> charges,
        CancellationToken cancellationToken = default)
    {
        return patientInvoiceService.GenerateInvoice(accountId, clientId, charges);
    }

    public Task<List<PatientInvoiceViewModel>> GetPreviousInvoicesAsync(
        int accountId,
        int clientId,
        string invoiceNo,
        CancellationToken cancellationToken = default)
    {
        return patientInvoiceService.GetPreviousInvoices(accountId, clientId, invoiceNo);
    }

    public Task<(List<InvoiceDetailsModel> Data, List<ClaimFilterOptionModel> UserList, int TotalCount)> GetInvoiceDetailsAsync(
        PendingCollectionFilters filters,
        CancellationToken cancellationToken = default)
    {
        return patientInvoiceService.GetInvoiceDetails(filters);
    }

    public Task<(byte[] PdfData, List<string> ErrorList)> GetInvoicePdfAsync(
        int accountId,
        int clientId,
        string invoiceNo,
        CancellationToken cancellationToken = default)
    {
        return patientInvoiceService.GetInvoicePDF(accountId, clientId, invoiceNo);
    }
}
