using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimNoteService
    {
        Task<ActionResponse> GetAllAsync(ClaimNoteGetAllModel model);
        Task<ActionResponse> AddAsync(ClaimNoteSaveModel model);
        Task<ActionResponse> AddToClaimsAsync(ClaimNoteRequestModel model);
        Task<ActionResponse> DeleteAsync(ClaimNoteDeleteModel model);
    }
}