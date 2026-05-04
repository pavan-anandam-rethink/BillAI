using BillingService.Domain.Models;
using BillingService.Domain.Models.Clients;
using BillingService.Domain.Models.Clients.History;

using BillingService.Domain.Models.PatientInvoice;
using Rethink.Services.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Client
{
    public interface IClientChargeHistoryService
    {
        Task<List<int>> GetClientHistoryClaimAsync(int accountInfoId);
        Task<ClientHistoryResponseModel> GetClientRecordAsync(ClientHistoryRequest requestModel, ClientRecordFilterModel filterModel);
        Task<ClientHistoryChargeDetailsResponse> GetClientChargeHistoryDetailsAsync(ClientHistoryChargeDetailsRequest ClientHistoryChargeDetailsRequest, ClientHistoryChargeFilterModel ClientHistoryChargeFilterModel);
        Task<List<AuthorizationNumberResponse>> GetAllAuthorizationNumbersAsync(UserInfo model);
        Task<InvoiceHistoryResponseModel> InvoicesSearchAsync(InvoiceHistoryRequest requestModel, InvoiceHistoryRequestFilterModel filterModel);
    }
}


