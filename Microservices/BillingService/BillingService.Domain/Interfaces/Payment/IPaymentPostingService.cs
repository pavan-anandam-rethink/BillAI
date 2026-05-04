using BillingService.Domain.Models;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Payment
{
    public interface IPaymentPostingService
    {
        Task<PaymentsResponseModel> GetAllPayments(GetPaymentsModel getPaymentsModel);
        List<PaymentMethodsModel> GetPaymentMethods();
        List<string> GetReconcileStatuses();
        Task<PaymentSummary> GetPaymentSummaryAsync(int paymentId);
        Task UpdateManualPaymentSummaryAsync(UpdateManualPaymentSummary model);
        Task<PaymentShortInfo> GetPaymentShortInfoAsync(int paymentId);
        Task<int> PostManualPaymentAsync(int paymentId);
        Task<int> CreateManualPatientPaymentAsync(ManualCreatePaymentModel model);
        Task<List<int>> DeletePaymentAsync(int[] paymentIds, int memberId,int AccountInfoId);
        Task<List<int>> ReconcilePaymentAsync(int[] paymentIds, int memberId);
        Task<int> ReconcileClaimAsync(int[] paymentId, int claimId, int memberId);
        Task<EOBPaymentInfo> GetEOBPaymentInfoAsync(int paymentId);
        Task UpdatePaymentSummaryAsync(UpdatePaymentSummary model);
        Task<string> GetNextPaymentID(int accountInfoId);
        Task<int> UploadFileAsync(EraUploadModelWithUserInfo model);
        Task DeleteUploadAsync(IdWithUserInfo model);
        Task<List<PaymentProcessingModel>> GetProcessingPaymentsAsync(UserInfo userInfo);
        Task<PaymentAttachmentReturnModel> GetUploadAsync(IdWithUserInfo model);
        Task StartPaymentParsingAsync(IdWithUserInfo model);
        Task HideProcessingInfoAsync(HideProcessingInfoModelWithUserInfo model);

        Task<string> GetFunderIdByPaymentIdAsync(int paymentId);
        Task<FunderDropdownResponseModel> GetAssignedFundersAsync(FunderSearchModelWithUserInfo funderSearchModel);
        List<ClaimTransactionModel> PrepareClaimTransactions(List<ClaimTransactionModel> claimTransactionData, List<int> serviceLineIds, int paymentTypes);
        Task<string> GetERAErrors(ERAUploadModel paymentIds);
        Task AddUnAllocatedPayments(UnAllocatedPaymentsModel model);
        Task<UnAllocatedPaymentsModel> GetUnAllocatedPaymentsById(UnAllocatedPaymentRequestModel model);
        Task<RethinkGuarantorDetails.ClientModel> GetGuarantorDetailsById(ClientHistoryUserInfo model);
        Task<ChildProfileEntityModel> GetPatientAccountDetails(int accountId, int patientId);
    }

}