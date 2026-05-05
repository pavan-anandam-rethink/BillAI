using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IPaymentClaimService
    {
        Task<ChildProfileInfo> getPatientDetails(int patientId, int accountInfoId);
        Task<PaymentClaimsResponseModel> GetPaymentClaimsAsync(GetClaimFilterModel getPaymentClaimsModel);
        Task<List<PaymentPaitentModel>> GetPatientsByPaymentAsync(int paymentId);
        Task<PatientPaymentClaimsResponseModel> GetPaymentClaimsByPatientsAsync(GetClaimsModel getPaymentClaimsModel);
        Task<PatientPaymentClaimsResponseModel> GetPaymentClaimsByPatientsAsyncNew(GetClaimsModel getPaymentClaimsModel);
        Task<List<PaymentClaimServiceLineModel>> GetPaymentClaimServiceLinesAsync(int claimId);

        //Task<List<PaymentClaimServiceLineModel>> GetPatientPaymentClaimServiceLinesAsync(GetPatientPaymentServiceLinesModel model);
        Task<List<PaymentClaimServiceLineModel>> GetPatientPaymentLinkedServiceLinesAsync(GetPatientPaymentServiceLinesModel model, bool isPatientDetailsLoading = false);
        Task<List<PaymentClaimServiceLineModel>> GetPatientPaymentUnlinkedServiceLinesAsync(GetPatientPaymentServiceLinesModel model);

        Task<List<PaymentClaimServiceLineModel>> GetPatientPaymentLinkedServiceLinesAsyncNew(GetPatientPaymentServiceLinesModel model, bool isPatientDetailsLoading = false);
        Task<List<PaymentClaimServiceLineModel>> GetPatientPaymentUnlinkedServiceLinesAsyncNew(GetPatientPaymentServiceLinesModel model);


        Task<PaymentClaimModel> GetPaymentClaimAsync(int claimId);
        Task<List<AddPatientResponseModel>> CreatePaymentClaimsAsync(CreatePatientClaimsModel patientClaimsModel);
        Task<int> CreateClaimsToEraAsync(CreateEraClaimsModel model);
        Task<PaymentClaimServiceLineModel> GetPaymentClaimServiceLineAsync(int serviceLineId);
        Task<IQueryable<PaymentClaimEntity>> GetPaymentClaimsByIdsAsync(int paymentId, List<int> claimsIds);
        Task UpdatePaymentClaimServiceLineAmountsAsync(UpdatePaymentServiceLineAmountsModelWithUserInfo modelWithUserInfo);
        Task PostPaymentClaimLines(PostPaymentClaimsModel model);
        Task<string> PostPatientPaymentClaimLinesAsync(PostRemovePatientClaimsModel model);
        Task<byte[]> GetEOBPaymentClaimPDFAsync(GetEOBClaimsModel model);
        Task<List<ClaimEOBInfoModel>> GetEOBClaimsAsync(int paymentId, List<int> claimIds);
        Task<PaymentClaimErrorsResponseModel> GetPaymentClaimErrorsAsync(GetByIdSortFilterWithUserInfo model);
        Task RemoveSelectedClaimsAsync(RemovePaymentClaimsModel model);
        Task RemoveSelectedPatientClaimsAsync(PostRemovePatientClaimsModel model);
        Task RemoveSelectedPatientPaymentAmountsAsync(PostRemovePatientClaimsModel model);
        void UpdateWithoutSavePaymentClaim(PaymentClaimEntity paymentClaim);
        Task<ClientPrintData> GetCompanyAccountInfoByPatientId(GetClientPrintDataRequest model);
        Task<IQueryable<GetPaymentClaimServiceLinesSmall>> GetPaymentClaimServiceLinesSmallAsync(GetChargeDetailsModel model);
        Task<List<BasicChargeDetails>> GetAllPaymentChargeIds(CreateInvoiceFilters model);
        Task<List<PatientPaymentClaimFullModel>> GetGroupedByPaymentsForPatientInvoice(List<int> chargeIds);
        Task<List<PatientPaymentClaimFullModel>> GetGroupedByPayments(PaymentEntity payment, GroupByParam groupby, bool isLinked, int childProfileId = 0);
        Task<List<PaymentGroupedModel>> GetAllCharges(int paymentId);
        Task<List<PatientPaymentClaimFullModel>> GetGroupedByPayments(List<PaymentGroupedModel> allChargeData, PaymentEntity payment, GroupByParam groupby, bool isLinked = false);
    }
}