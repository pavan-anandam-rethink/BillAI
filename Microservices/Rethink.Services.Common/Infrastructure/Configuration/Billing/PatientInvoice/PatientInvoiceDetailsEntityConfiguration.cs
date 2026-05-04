using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.PatientInvoice
{
    [ExcludeFromCodeCoverage]
    public class PatientInvoiceDetailsEntityConfiguration : IEntityTypeConfiguration<PatientInvoiceDetailsEntity>
    {
        public void Configure(EntityTypeBuilder<PatientInvoiceDetailsEntity> builder)
        {
            builder.ToTable("PatientInvoiceDetails","dbo")
                   .HasKey(x => x.Id);

            builder.HasOne(x => x.PatientInvoiceEntity)
                   .WithMany(x => x.PatientInvoiceDetailsEntity)
                   .HasForeignKey(x => x.InvoiceId)
                   .HasConstraintName("FK_PatientInvoice_PatientInvoiceDetails");

            builder.HasOne(x => x.ChargeEntry)
                   .WithMany(x => x.PatientInvoiceDetailsEntity)
                   .HasForeignKey(x => x.ChargeId)
                   .HasConstraintName("FK_ClaimChargeEntry_PatientInvoiceDetails");
        }
    }
}
