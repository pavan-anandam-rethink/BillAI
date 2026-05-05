using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.PatientInvoice
{
    [ExcludeFromCodeCoverage]
    public class PatientInvoiceEntityConfiguration : IEntityTypeConfiguration<PatientInvoiceEntity>
    {
        public void Configure(EntityTypeBuilder<PatientInvoiceEntity> builder)
        {
            builder.ToTable("PatientInvoice", "dbo")
                   .HasKey(x => x.Id);

            builder.HasMany(p => p.PatientInvoiceDetailsEntity)
                .WithOne(d => d.PatientInvoiceEntity)
                .HasForeignKey(d => d.Id);
        }

    }
}
