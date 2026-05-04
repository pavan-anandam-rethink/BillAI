using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    public class ClaimBillingProviderEntityConfiguration : IEntityTypeConfiguration<ClaimBillingProviderEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimBillingProviderEntity> builder)
        {
            builder.ToTable("ClaimBillingProvider");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ClaimId).IsRequired();

            builder.Property(x => x.ProviderType)
                   .HasMaxLength(10)
                   .IsRequired();

            builder.Property(x => x.FirstName)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.Property(x => x.LastNameOrFacilityName)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.NPI)
                   .HasMaxLength(10)
                   .IsRequired();

            builder.Property(x => x.TaxId)
                   .HasMaxLength(20)
                   .IsRequired(false);

            builder.Property(x => x.TaxonomyCode)
                   .HasMaxLength(20)
                   .IsRequired(false);

            builder.Property(x => x.AddressLine1)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.AddressLine2)
                   .HasMaxLength(200)
                   .IsRequired(false);

            builder.Property(x => x.City)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.State)
                   .HasMaxLength(2)
                   .IsRequired();

            builder.Property(x => x.Zip)
                   .HasMaxLength(5)
                   .IsRequired();

            builder.Property(x => x.ZipExt)
                   .HasMaxLength(4)
                   .IsRequired();

            builder.Property(x => x.DateCreated)
                   .HasDefaultValueSql("SYSUTCDATETIME()")
                   .IsRequired();

            builder.Property(x => x.CreatedBy)
                   .IsRequired();

            builder.Property(x => x.DateLastModified)
                   .IsRequired(false)
                   .HasColumnType("datetime");

            builder.Property(x => x.ModifiedBy)
                   .IsRequired(false);

            builder.Property(x => x.DateDeleted)
                   .IsRequired(false)
                   .HasColumnType("datetime");

            builder.Property(x => x.DeletedBy)
                   .IsRequired(false);

            builder.HasOne(x => x.Claim)
                   .WithMany(c=>c.ClaimBillingProviders) // or .WithMany(c => c.BillingProviders)
                   .HasForeignKey(x => x.ClaimId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ClaimId)
                   .HasDatabaseName("IX_ClaimBillingProvider_ClaimId");
        }
    }
}

