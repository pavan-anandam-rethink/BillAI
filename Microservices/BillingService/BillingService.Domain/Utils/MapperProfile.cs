using AutoMapper;
using Billing.FolderStructure.Core.Models;
using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Extensions;
using BillingService.Domain.Models;
using BillingService.Domain.Models.BillingSettings;
using BillingService.Domain.Models.PaymentClaimServiceLine;
using BillingService.Domain.Models.PaymentClaimServiceLineAdjustment;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Dtos.Billing;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.EligibilityRequest;
using Rethink.Services.Common.Models.ReportingModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BillingService.Domain.Utils
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            LoadBillingMapping();
        }

        private void LoadBillingMapping()
        {
            CreateMap<ClaimItem, ClaimModel>();
            CreateMap<ClaimItemWithPayments, ClaimModelWithPayments>();

            CreateMap<AppointmentItem, AppointmentModel>()
                .ForMember(x => x.TimeRange, m => m.MapFrom(z => $"{z.StartDate.ToString("HH:mm tt")}-{z.EndDate.ToString("HH:mm tt")} {z.StartDate.ToString("MM/dd/yy")}"))
                .ForMember(x => x.ServiceName, m => m.MapFrom(z => z.ServiceName))
                .ForMember(x => x.BillingCode, m => m.MapFrom(z => z.BillingCode))
                .ForMember(x => x.BillingCode2, m => m.MapFrom(z => z.BillingCode2));
            CreateMap<ChargePaymentItem, ChargePaymentModel>();
            CreateMap<ChargePaymentModel, ChargePaymentItem>();


            CreateMap<ClaimEntity, ClaimItem>()
                .ForMember(x => x.LocationName, m => m.MapFrom(z => z.ToLocation ?? string.Empty))
                .ForMember(x => x.LocationId, m => m.MapFrom(z => z.ToLocationId))

                .ForMember(x => x.HasAppointmentLinks, m => m.MapFrom(z => z.ClaimAppointmentLinks != null ? z.ClaimAppointmentLinks.Count > 0 : false))
                .ForMember(x => x.TotalCharges, m => m.MapFrom(z => z.ClaimChargeEntries != null ? z.ClaimChargeEntries.Where(c => !c.DateDeleted.HasValue).Sum(c => c.Charges) : 0))
                .ForMember(x => x.PaidAmount, m => m.MapFrom(z => z.ClaimChargeEntries != null ? z.ClaimChargeEntries.SelectMany(ce => ce.ChargePayments).Where(cp => cp.DateDeleted == null).Sum(cp => cp.Amount) : 0))
                .ForMember(x => x.Status, m => m.MapFrom(z => z.ClaimStatus));
            CreateMap<ClaimEntity, ClaimItemWithPayments>()
                .ForMember(x => x.LocationName, m => m.MapFrom(z => z.ToLocation ?? string.Empty))
                .ForMember(x => x.LocationId, m => m.MapFrom(z => z.ToLocationId))
                .ForMember(x => x.HasAppointmentLinks, m => m.MapFrom(z => z.ClaimAppointmentLinks != null ? z.ClaimAppointmentLinks.Count > 0 : false))
                .ForMember(x => x.TotalCharges, m => m.MapFrom(z => z.ClaimChargeEntries != null ? z.ClaimChargeEntries.Where(c => !c.DateDeleted.HasValue).Sum(c => c.Charges) : 0))
                .ForMember(x => x.PaidAmount, m => m.MapFrom(z => z.ClaimChargeEntries != null ? z.ClaimChargeEntries.SelectMany(ce => ce.ChargePayments).Where(cp => cp.DateDeleted == null).Sum(cp => cp.Amount) : 0))
                .ForMember(x => x.ClaimChargeInfoItems,
                    m => m.MapFrom(z => z.ClaimChargeEntries != null ? z.ClaimChargeEntries.Where(c => !c.DateDeleted.HasValue).Select(ce =>
                                                              new ClaimChargeInfoItem
                                                              {
                                                                  BillingCode = ce.BillingCode,
                                                                  DateOfService = ce.DateOfService,
                                                                  Modifier1 = ce.Modifier1,
                                                                  Modifier2 = ce.Modifier2,
                                                                  TotalCharge = ce.Charges,
                                                                  TotalPaid = ce.ChargePayments != null ? ce.ChargePayments.Where(cp => cp.DateDeleted == null).Sum(cp => cp.Amount) : 0,
                                                                  UnitRate = ce.UnitRate ?? ce.Charges / (ce.Units > 0 ? ce.Units : 1),
                                                                  Units = (ce.Units > 0 ? ce.Units : 1)
                                                              }
                                                            ) : new List<ClaimChargeInfoItem>()))

                .ForMember(x => x.StatusName, m => m.MapFrom(z => z.ClaimStatus == ClaimStatus.PendingReview ? "Pending Review" :
                                                                  z.ClaimStatus == ClaimStatus.Void ? "Void" :
                                                                  z.ClaimStatus == ClaimStatus.VoidClosed ? "VoidClosed" :
                                                                  z.ClaimStatus == ClaimStatus.Billed ? "Billed" :
                                                                  z.ClaimStatus == ClaimStatus.Pending ? "Pending" :
                                                                  z.ClaimStatus == ClaimStatus.Denied ? "Denied" :
                                                                  z.ClaimStatus == ClaimStatus.Closed ? "Closed" :
                                                                  z.ClaimStatus == ClaimStatus.ReadyToBill ? "Ready To Bill" :
                                                                  z.ClaimStatus == ClaimStatus.RejectedClearinghouse ? "Rejected - Clearinghouse" :
                                                                  z.ClaimStatus == ClaimStatus.RejectedFunder ? "Rejected - Funder" :
                                                                  z.ClaimStatus == ClaimStatus.Rebill ? "Rebill" :
                                                                  z.ClaimStatus == ClaimStatus.BillNextFunder ? "Bill Next Funder" :
                                                                  z.ClaimStatus == ClaimStatus.Paid ? "Paid"
                                                                                                                     //z.ClaimStatus == ClaimStatus.Flagged       ? "Flagged": // TODO: use claim.IsFlagged instead
                                                                                                                     : string.Empty));

            CreateMap<ClaimAttachmentEntity, ClaimAttachmentItem>();

            CreateMap<AppointmentRethinkModel, AppointmentItem>().ForMember(x => x.StartDate, m => m.MapFrom(z => z.startDate.Date.AddMinutes(z.actualStartTime ?? z.startTime)))
                                                           .ForMember(x => x.EndDate, m => m.MapFrom(z => z.startDate.Date.AddMinutes(z.actualEndTime ?? z.endTime)))
                                                           .ForMember(x => x.StaffId, m => m.MapFrom(z => z.StaffMember.memberId))
                                                           .ForMember(x => x.StaffName,
                                                                      m => m.MapFrom(z => z.StaffMember != null && z.StaffMember.Member != null ?
                                                                        FullNameExt.GetFullName(z.StaffMember.Member.firstName,
                                                                        z.StaffMember.Member.middleName,
                                                                        z.StaffMember.Member.lastName) :
                                                                        string.Empty))
                                                           .ForMember(x => x.Location,
                                                                      m => m.MapFrom(z => z.PlaceOfService != null ? z.PlaceOfService.code + " - " + z.PlaceOfService.description
                                                                      : string.Empty))
                                                           .ForMember(x => x.ServiceName, m => m.MapFrom(z => z.ProviderService.name))
                                                           .ForMember(x => x.ProviderServiceName, m => m.MapFrom(z => z.ProviderService.name))
                                                           .ForMember(x => x.ServiceLocation, m => m.MapFrom(z => z.Location.name))
                                                           .ForMember(x => x.BillingCode, m => m.MapFrom(z => z.ChildProfileAuthorizationBillingCode != null
                                                           ? z.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode.billingCode
                                                           : (z.ProviderBillingCode != null ? z.ProviderBillingCode.billingCodeText : string.Empty)))
                                                           .ForMember(x => x.BillingCode2, m => m.MapFrom(z => z.ChildProfileAuthorizationBillingCode != null
                                                           ? z.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode.billingCode2
                                                           : (z.ProviderBillingCode != null ? z.ProviderBillingCode.billingCodeText : string.Empty)));
            //*****************************************************************************************************************************************************************************************************************


            CreateMap<ChargePaymentEntity, ChargePaymentItem>()
                .ForMember(x => x.Date, m => m.MapFrom(z => z.DateCreated))
                .ForMember(x => x.CPTCode, m => m.MapFrom(z => z.ChargeEntry != null ? string.Format("{0} - {1:MM/dd/yyyy}", z.ChargeEntry.BillingCode, z.ChargeEntry.DateOfService) : string.Empty))
                .ForMember(x => x.ReasonCode, m => m.MapFrom(z => z.ReasonCode != null ? z.ReasonCode.name : string.Empty))
                .ForMember(x => x.PaymentMethod, m => m.MapFrom(z => z.PaymentMethod != null ? z.PaymentMethod.Name : string.Empty))
                .ForMember(x => x.PostedBy, m => m.MapFrom(z => z.CreatedMember != null ? z.CreatedMember.firstName + " " + z.CreatedMember.lastName : string.Empty))
                ;

            CreateMap<ManualCreatePaymentModel, PaymentEntity>()
                .ForMember(dest => dest.PaymentMethodId, (opt => opt.Ignore()));
            CreateMap<PaymentMethodEntity, PaymentMethodModel>();
            CreateMap<PaymentEntity, PaymentDataForExpected>()
                .ForMember(dest => dest.FunderId, (opt => opt.MapFrom(src => src.FunderID)));

            CreateMap<PaymentClaimServiceLineAdjustmentEntity, PaymentClaimServiceLineAdjustmentModel>()
               .ForMember(x => x.serviceLineId, m => m.MapFrom(z => z.PaymentClaimServiceLineId))
               .ForMember(x => x.Id, m => m.MapFrom(z => z.Id))
               .ForMember(x => x.Amount, m => m.MapFrom(z => z.AdjustmentAmount))
               .ForMember(x => x.GroupCode, m => m.MapFrom(z => z.AdjustmentGroupCode))
               .ForMember(x => x.ReasonCode, m => m.MapFrom(z => z.AdjustmentReasonCode))
               .ForMember(x => x.isPositive, m => m.MapFrom(z => z.IsAdjustmentPositive))
               .ForMember(x => x.PostDate, m => m.MapFrom(z => z.DateLastModified))
               .ForMember(x => x.PaymentId, m => m.MapFrom(z => z.PaymentClaimServiceLine.PaymentClaimId));


            CreateMap<AddOrEditAdjustmentModelForBulkPosting, AddOrEditAdjustmentModel>();

            CreateMap<CarcCodeEntity, CarcCodeResponseModel>();

            CreateMap<ChildProfileEntityModel, BaseNameOption>()
                .ForMember(x => x.Id, m => m.MapFrom(z => z.Id))
                .ForMember(x => x.Name, m => m.MapFrom(z => $"{z.FirstName} {z.MiddleName} {z.LastName}"));

            CreateMap<UnprocessedAppointmentsRequestModel, UnbilledAppointmentsRequestModel>();

            CreateMap<Eligibility270Request, Eligibility270DTO>();

            CreateMap<BillingFunderSettingRequestModel, FunderSettingsEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AccountInfoId, opt => opt.MapFrom(src => src.AccountInfoId))
                .ForMember(dest => dest.FunderName, opt => opt.MapFrom(src => src.FunderName))
                .ForMember(dest => dest.FunderId, opt => opt.MapFrom(src => src.FunderId))
                .ForMember(dest => dest.ClaimFilingIndicatorId, opt => opt.MapFrom(src => src.ClaimFilingIndicatorId))
                .ForMember(dest => dest.IncludeTaxonomyCode, opt => opt.MapFrom(src => src.IncludeTaxonomyCode))
                .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.DateLastModified, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<StateEntity, StateDto>()
                .ForMember(dest => dest.StateId, opt => opt.MapFrom(src => src.Id));

            CreateMap<ClaimEdiFilesModel, ClaimEdiFilesEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<ExternalCodeEntity, ExternalCodeResponseModel>();
        }
    }
}