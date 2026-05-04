using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class Eligibility271ResponseEntityConfiguration
        : IEntityTypeConfiguration<Eligibility271ResponseEntity>
    {
        public void Configure(EntityTypeBuilder<Eligibility271ResponseEntity> builder)
        {
            builder.ToTable("Eligibility271Response", "dbo");
            builder.HasKey(x => x.Eligibility271ResponseId);

            // Ignore base class properties that don't exist in this table
            builder.Ignore(x => x.Id);

            builder.Property(x => x.Eligibility271ResponseId)
                   .ValueGeneratedOnAdd();
            builder.Ignore(x => x.Id);

            builder.Property(x => x.TransactionControlNumber)
                   .IsRequired()
                   .HasColumnType("uniqueidentifier");
            builder.Property(x => x.FunderId);

            builder.Property(x => x.EffectiveStartDate)
                   .HasColumnType("date");

            builder.Property(x => x.EffectiveEndDate)
                   .HasColumnType("date");

            builder.Property(x => x.CoverageStatus)
                   .HasMaxLength(50);

            builder.Property(x => x.SubscriberStartDate)
                   .HasColumnType("date");

            builder.Property(x => x.SubscriberEndDate)
                   .HasColumnType("date");

            builder.Property(x => x.CreatedBy)
                   .IsRequired();
            builder.Property(x => x.CreatedDate)
                   .HasColumnType("datetime2(3)")
                   .HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(x => x.ModifiedBy);
            builder.Property(x => x.ModifiedDate)
                   .HasColumnType("datetime2(3)");
        }
    }
}
