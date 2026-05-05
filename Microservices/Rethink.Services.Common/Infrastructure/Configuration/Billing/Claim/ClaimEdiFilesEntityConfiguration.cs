using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimEdiFilesEntityConfiguration : IEntityTypeConfiguration<ClaimEdiFilesEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimEdiFilesEntity> builder)
        {
            builder.ToTable("ClaimEdiFilesPath");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AccountInfoId)
                   .IsRequired();

            builder.Property(x => x.FileType)
                   .HasMaxLength(10)
                   .IsUnicode(false)
                   .IsRequired();

            builder.Property(x => x.ClaimSubmissionId)
                   .IsRequired(false);

            builder.Property(x => x.ClaimId)
                   .IsRequired();

            builder.Property(x => x.PaymentId)
                   .IsRequired(false);

            builder.Property(x => x.BlobFilePath)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(x => x.DateCreated)
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSUTCDATETIME()")
                   .IsRequired();

            builder.Property(x => x.CreatedBy)
                   .IsRequired();

            builder.Property(x => x.DateLastModified)
                   .IsRequired(false)
                   .HasColumnType("datetime2");

            builder.Property(x => x.ModifiedBy)
                   .IsRequired(false);

            builder.Property(x => x.DateDeleted)
                   .IsRequired(false)
                   .HasColumnType("datetime2");

            builder.Property(x => x.DeletedBy)
                   .IsRequired(false);

            builder.HasOne(x => x.ClaimSubmission)
                   .WithMany()
                   .HasForeignKey(x => x.ClaimSubmissionId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => new { x.AccountInfoId, x.ClaimId })
                .HasDatabaseName("IX_ClaimEdiFiles_AccountInfoId_ClaimId");
        }
    }
}
