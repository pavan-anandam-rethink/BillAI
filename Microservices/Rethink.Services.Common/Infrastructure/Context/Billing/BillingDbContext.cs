using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Feature;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Infrastructure.Configuration.Billing;
using Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim.History;
using Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim.WriteOff;
using Rethink.Services.Common.Infrastructure.Configuration.Billing.Era;
using Rethink.Services.Common.Infrastructure.Configuration.Billing.PatientInvoice;
using Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment;
using Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting;
using Rethink.Services.Common.Infrastructure.Configuration.History;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Infrastructure.Context.Billing
{
    [ExcludeFromCodeCoverage]
    public class BillingDbContext : BaseDbContext<BillingDbContext>
    {

        public BillingDbContext(DbContextOptions<BillingDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<PaymentClaimEntity> PaymentClaims { get; set; }
        public virtual DbSet<PaymentClaimAdjustmentEntity> PaymentClaimAdjustments { get; set; }
        public virtual DbSet<PaymentClaimAttachmentEntity> PaymentClaimAttachments { get; set; }
        public virtual DbSet<PaymentClaimAttachmentTypeEntity> PaymentClaimAttachmentTypes { get; set; }
        public virtual DbSet<PaymentClaimServiceLineEntity> PaymentClaimServiceLines { get; set; }
        public virtual DbSet<PaymentEntity> Payments { get; set; }
        public virtual DbSet<PaymentTypeEntity> PaymentTypes { get; set; }
        public virtual DbSet<PaymentClaimServiceLineAdjustmentEntity> PaymentClaimServiceLineAdjustments { get; set; }
        public virtual DbSet<PaymentErrorEntity> PaymentErrors { get; set; }
        public virtual DbSet<PaymentMethodEntity> PaymentMethods { get; set; }
        public virtual DbSet<PaymentClaimErrorEntity> PaymentClaimErrors { get; set; }
        public virtual DbSet<PaymentClaimServiceLineErrorEntity> PaymentClaimServiceLineErrors { get; set; }
        public virtual DbSet<PaymentAdjustmentReasonEntity> PaymentAdjustmentReasons { get; set; }
        public virtual DbSet<ClaimSubmissionFunderSequenceEntity> ClaimSubmissionFunderSequence { get; set; }
        public virtual DbSet<UnAllocatedPaymentEntity> UnAllocatedPayments { get; set; }

        // claim
        public virtual DbSet<ClaimChargeEntryEntity> ClaimChargeEntries { get; set; }
        public virtual DbSet<ClaimEntity> Claims { get; set; }
        public virtual DbSet<ClaimAttachmentEntity> ClaimAttachments { get; set; }
        public virtual DbSet<ClaimHistoryEntity> ClaimHistory { get; set; }
        public virtual DbSet<ClaimVersionEntity> ClaimVersionEntity { get; set; }
        public virtual DbSet<ClaimSubmissionEntity> ClaimSubmissions { get; set; }
        public virtual DbSet<ClaimSubmissionServiceLineEntity> ClaimSubmissionServiceLines { get; set; }
        public virtual DbSet<ClaimValidationErrorEntity> ClaimValidationErrors { get; set; }
        public virtual DbSet<ClaimSearchFunderEntity> ClaimSearchFunders { get; set; }
        public virtual DbSet<ClaimSearchLocationEntity> ClaimSearchLocations { get; set; }
        public virtual DbSet<ClaimSearchChildProfileAuthorizationEntity> ClaimSearchChildProfileAuthorizations { get; set; }
        public virtual DbSet<ClaimSearchClientEntity> ClaimSearchClients { get; set; }
        public virtual DbSet<ClaimSearchRenderingProviderEntity> ClaimSearchRenderingProviders { get; set; }
        public virtual DbSet<ClaimAppointmentLinkEntity> ClaimAppointmentLinks { get; set; }
        public virtual DbSet<ClaimErrorMessageEntity> ClaimErrorMessages { get; set; }
        public virtual DbSet<ClaimErrorCategoryEntity> ClaimErrorCategory { get; set; }
        public virtual DbSet<UnProcessedApointmentScheduleEntity> UnProcessedApointmentSchedule { get; set; }

        public virtual DbSet<ClaimFlagReasonMaster> ClaimFlagReasonMaster { get; set; }
        public virtual DbSet<ClaimFlagTransaction> ClaimFlagTransaction { get; set; }
        public virtual DbSet<Eligibility271ResponseEntity> Eligibility271Responses { get; set; }
        public virtual DbSet<ClaimBillingProviderEntity> ClaimBillingProviders { get; set; }
        public virtual DbSet<StateEntity> States { get; set; }
        public virtual DbSet<TimezonesEntity> TimeZones { get; set; }

        //Write Off Tables
        public virtual DbSet<WriteOffActionEntity> WriteOffAction { get; set; }
        public virtual DbSet<WriteOffApplicationEntity> WriteOffApplication { get; set; }
        public virtual DbSet<WriteOffReasonCodeEntity> WriteOffReasonCode { get; set; }
        public virtual DbSet<ClaimWriteOffEntity> ClaimWriteOff { get; set; }
        public virtual DbSet<ClaimChargeEntryWriteOffEntity> ClaimChargeEntryWriteOff { get; set; }

        //Patient Invoice
        public virtual DbSet<PatientInvoiceEntity> PatientInvoices { get; set; }
        public virtual DbSet<PatientInvoiceDetailsEntity> PatientInvoiceDetails { get; set; }
        public virtual DbSet<PatientGuarantorEntity> PatientGuarantors { get; set; }

        // X12 Codes
        public virtual DbSet<ExternalCodeEntity> ExternalCodes { get; set; }

        public virtual DbSet<MemberViewSettingEntity> MemberViewSetting { get; set; }

        // Claim Submission- Claim Filing Indicator
        public virtual DbSet<ClaimFilingIndicatorEntity> ClaimFilingIndicator { get; set; }
        public virtual DbSet<FunderSettingsEntity> FunderSettings { get; set; }
        public virtual DbSet<AppointmentClaimProcessingErrorEntity> AppointmentClaimProcessingError { get; set; }

        //Feature
        public virtual DbSet<FeatureEntity> Features { get; set; }
        public virtual DbSet<AccountFeatureSettingEntity> AccountFeatureSettings { get; set; }

        //AuditLogEntity
        public virtual DbSet<AuditLogEntity> AuditLogEntity { get; set; }



        // EDI Files
        public virtual DbSet<ClaimEdiFilesEntity> ClaimEdiFiles { get; set; }

        public override Task LogStoredProcedureParameters(string storedProcedureName, string parameters)
        {
            throw new System.NotImplementedException();
        }

        public virtual DbSet<BillingSettingInformationEntity> BillingSettingInformation { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // billing domain models
            modelBuilder.ApplyConfiguration(new PaymentClaimEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentClaimAdjustmentEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentAttachmentEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentClaimAttachmentEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentClaimAttachmentTypeEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentClaimServiceLineEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentTypesConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentNoteEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentEraUploadEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentClaimServiceLineAdjustmentEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentEraUploadEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentErrorEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentClaimErrorEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentClaimServiceLineErrorEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentAdjustmentReasonConfiguration());

            modelBuilder.ApplyConfiguration(new ExternalCodeConfiguration());

            modelBuilder.ApplyConfiguration(new ClaimEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimSubmissionEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimSubmissionServiceLineEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimSubmissionFunderSequenceEntityConfiguration());
            modelBuilder.ApplyConfiguration(new UnAllocatedPaymentEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimErrorCategoryEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimErrorMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimValidationErrorEntityConfiguration());
            modelBuilder.ApplyConfiguration(new EraValidationErrorEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimHistoryEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimHistoryActionEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimVersionEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimChargeEntryEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimDiagnosisCodeEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimAppointmentLinkEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimAppointmentLinkChargeEntryConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimNoteEntityConfiguration());
            modelBuilder.ApplyConfiguration(new CarcCodeEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimFlagTransactionEntityConfiguration());
            modelBuilder.ApplyConfiguration(new Eligibility271ResponseEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimBillingProviderEntityConfiguration());
            modelBuilder.ApplyConfiguration(new StateEntityConfiguration());

            modelBuilder.ApplyConfiguration(new TimeZonesEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ChargePaymentEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentMethodEntityConfiguration());

            modelBuilder.ApplyConfiguration(new ClaimAttachmentConfiguration());
            modelBuilder.ApplyConfiguration(new ClearingHouseResponseEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimEdiFilesEntityConfiguration());
            modelBuilder.ApplyConfiguration(new MemberViewSettingEntityConfiguration());

            //Write off table configuration
            modelBuilder.ApplyConfiguration(new WriteOffActionConfiguration());
            modelBuilder.ApplyConfiguration(new WriteOffApplicationConfiguration());
            modelBuilder.ApplyConfiguration(new WriteOffReasonCodeConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimWriteOffConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimChargeEntryWriteOffConfiguration());

            //Patient Invoice
            modelBuilder.ApplyConfiguration(new PatientInvoiceEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PatientInvoiceDetailsEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PatientGuarantorEntityConfiguration());

            //ArReport table Configuration
            modelBuilder.ApplyConfiguration(new ClaimTransactionEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ChargeTransactionEntityConfigurator());
            //Feature
            modelBuilder.ApplyConfiguration(new FeatureEntityConfiguration());
            modelBuilder.ApplyConfiguration(new AccountFeatureSettingEntityConfiguration());

            //AuditLog
            modelBuilder.ApplyConfiguration(new AuditLogEntityConfiguration());
            base.OnModelCreating(modelBuilder);

        }

    }
}