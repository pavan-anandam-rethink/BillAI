using BillingService.Domain.Models.PaymentPosting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Payment
{
    public interface IPaymentNoteService
    {
        Task<List<PaymentNote>> GetAll(int paymentId);
        Task<int> AddToPaymentsAsync(PaymentNoteSaveModel[] model);
        Task<int> AddNote(PaymentNoteSaveModel model);
        Task<int> DeleteNote(PaymentNoteDeleteModel model);
    }
}