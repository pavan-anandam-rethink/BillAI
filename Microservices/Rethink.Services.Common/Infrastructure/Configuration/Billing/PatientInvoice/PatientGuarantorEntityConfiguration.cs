using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.PatientInvoice
{
    public class PatientGuarantorEntityConfiguration : IEntityTypeConfiguration<PatientGuarantorEntity>
    {
        public void Configure(EntityTypeBuilder<PatientGuarantorEntity> builder)
        {
            builder.ToTable("PatientGuarantor", "dbo")
                  .HasKey(x => x.Id);

            builder.HasOne(x => x.PatientInvoice)
                   .WithMany(x => x.PatientGuarantors)
                   .HasForeignKey(x => x.InvoiceId)
                   .HasConstraintName("FK_PatientInvoice_PatientGuarantors")
                   .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
