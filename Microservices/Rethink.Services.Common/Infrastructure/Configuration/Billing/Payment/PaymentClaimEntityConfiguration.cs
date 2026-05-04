using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentClaimEntityConfiguration : IEntityTypeConfiguration<PaymentClaimEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentClaimEntity> builder)
        {
            builder.ToTable("PaymentClaim");

            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.ChildProfileId).HasColumnName("childProfileId");
            builder.Property(e => e.ClaimId).HasColumnName("hcClaimId");

            builder.Property(e => e.ClaimDateFrom)
                .HasColumnName("claimDateFrom")
                .HasColumnType("datetime");

            builder.Property(e => e.ClaimDateTo)
                .HasColumnName("claimDateTo")
                .HasColumnType("datetime");

            builder.Property(e => e.ClaimIdentifier)
                .HasColumnName("claimIdentifier")
                .HasMaxLength(100);

            builder.Property(e => e.ClaimIdentifierOrig)
                .HasColumnName("claimIdentifierOrig")
                .HasMaxLength(100);

            builder.Property(e => e.ClaimReceivedDate)
                .HasColumnName("claimReceivedDate")
                .HasColumnType("datetime");

            builder.Property(e => e.ClaimStatus)
                .HasColumnName("claimStatus")
                .HasMaxLength(10);

            builder.Property(e => e.ClaimStatusOrig)
                .HasColumnName("claimStatusOrig")
                .HasMaxLength(10);

            builder.Property(e => e.ClientFirstName)
                .HasColumnName("clientFirstName")
                .HasMaxLength(512);

            builder.Property(e => e.ClientIdentifier)
                .HasColumnName("clientIdentifier")
                .HasMaxLength(50);

            builder.Property(e => e.ClientLastName)
                .HasColumnName("clientLastName")
                .HasMaxLength(512);

            builder.Property(e => e.ClientMiddleName)
                .HasColumnName("clientMiddleName")
                .HasMaxLength(512);

            builder.Property(e => e.ControlNumber)
                .HasColumnName("controlNumber")
                .HasMaxLength(50);

            builder.Property(e => e.CreatedBy).HasColumnName("createdBy");

            builder.Property(e => e.DateCreated)
                .HasColumnName("dateCreated")
                .HasColumnType("datetime");

            builder.Property(e => e.DateDeleted)
                .HasColumnName("dateDeleted")
                .HasColumnType("datetime");

            builder.Property(e => e.DateLastModified)
                .HasColumnName("dateLastModified")
                .HasColumnType("datetime");

            builder.Property(e => e.FilingIndicator)
                .HasColumnName("filingIndicator")
                .HasMaxLength(10);

            builder.Property(e => e.FilingIndicatorOrig)
                .HasColumnName("filingIndicatorOrig")
                .HasMaxLength(10);

            builder.Property(e => e.RenderingProviderName)
                .HasColumnName("renderingProviderName")
                .HasMaxLength(200);

            builder.Property(e => e.PaymentId).HasColumnName("hcPaymentId");

            builder.Property(e => e.IsReviewed).HasColumnName("isReviewed");

            builder.Property(e => e.ModifiedBy).HasColumnName("modifiedBy");

            builder.Property(e => e.PatientRespAmount)
                .HasColumnName("patientRespAmount")
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.PatientRespAmountOrig)
                .HasColumnName("patientRespAmountOrig")
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.TotalCharge)
                .HasColumnName("totalCharge")
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.TotalChargeOrig)
                .HasColumnName("totalChargeOrig")
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.TotalPayment)
                .HasColumnName("totalPayment")
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.TotalPaymentOrig)
                .HasColumnName("totalPaymentOrig")
                .HasColumnType("decimal(18, 2)");

            builder.HasOne(d => d.Payment)
                .WithMany(p => p.PaymentClaims)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(d => d.Claim)
                .WithMany(c => c.PaymentClaims)
                .HasForeignKey(d => d.ClaimId);
        }
    }
}