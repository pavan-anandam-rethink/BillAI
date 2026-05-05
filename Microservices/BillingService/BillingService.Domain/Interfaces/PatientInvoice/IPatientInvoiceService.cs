using BillingService.Domain.Models;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.PatientInvoice
{
    public interface IPatientInvoiceService
    {
        Task<(IEnumerable<PatientInvoiceCreationModel> Data, int TotalCount)> GetPICreationDetails(CreateInvoiceFilters filters);
        Task<(byte[] PdfData, List<string> ErrorList)> GeneratePDF(List<InvoiceRequestModel> invoiceRequests, bool isSubmit, bool includePreviousInvoices, string invoiceNumber);
        Task<PatientInvoiceViewModel> GenerateInvoice(int accountId, int clientId, List<ChargeModel> charges);
        Task<List<PatientInvoiceViewModel>> GetPreviousInvoices(int accountId, int clientId, string invoiceNo);
        Task<(List<InvoiceDetailsModel> Data, List<ClaimFilterOptionModel> UserList, int TotalCount)> GetInvoiceDetails(PendingCollectionFilters filters);
        Task<(byte[] pdfData, List<string> ErrorList)> GetInvoicePDF(int accountId, int clientId, string InvoiceNo);
    }
}
