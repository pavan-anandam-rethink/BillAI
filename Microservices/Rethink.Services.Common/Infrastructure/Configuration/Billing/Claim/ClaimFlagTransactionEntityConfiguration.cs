using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimFlagTransactionEntityConfiguration : IEntityTypeConfiguration<ClaimFlagTransaction>
    {
        public void Configure(EntityTypeBuilder<ClaimFlagTransaction> builder)
        {
            // Table name
            builder.ToTable("ClaimFlagTransaction");

            // Primary Key
            builder.HasKey(x => x.Id);

            // Required fields
            builder.Property(x => x.AccountInfoId)
                   .IsRequired();
            builder.Property(x => x.HcClaimId)
                   .IsRequired();
            builder.Property(x => x.ReasonId)
                   .IsRequired();
            builder.Property(x => x.ActionType)
                   .IsRequired()
                   .HasMaxLength(50);
            builder.Property(x => x.DateCreated)
                   .IsRequired();
            builder.Property(x => x.CreatedBy)
                   .IsRequired();

            // FK → Claim
            builder.HasOne(x => x.Claim)
                   .WithMany() // or .WithMany(c => c.ClaimFlagTransactions)
                   .HasForeignKey(x => x.HcClaimId)
                   .OnDelete(DeleteBehavior.Restrict);

            // FK → Reason
            builder.HasOne(x => x.Reason)
                   .WithMany()
                   .HasForeignKey(x => x.ReasonId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Ignore soft-delete navigation confusion
            builder.Property(x => x.DateDeleted)
                   .IsRequired(false);
            builder.Property(x => x.ModifiedBy)
                   .IsRequired(false);
            // Optional: index parity with DB
            builder.HasIndex(x => x.HcClaimId)
                   .HasDatabaseName("IX_ClaimFlagTransaction_HcClaimId");

            builder.HasIndex(x => x.AccountInfoId)
                   .HasDatabaseName("IX_ClaimFlagTransaction_AccountInfoId");

            builder.HasIndex(x => x.DateCreated)
                   .HasDatabaseName("IX_ClaimFlagTransaction_CreatedDate");
        }
    }
}
