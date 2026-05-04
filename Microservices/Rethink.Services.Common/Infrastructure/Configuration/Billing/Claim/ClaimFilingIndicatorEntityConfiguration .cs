using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    public class ClaimFilingIndicatorEntityConfiguration : IEntityTypeConfiguration<ClaimFilingIndicatorEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimFilingIndicatorEntity> builder)
        {
            builder.ToTable("ClaimFilingIndicator");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(2);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.DateCreated)
                .IsRequired();

            builder.Property(x => x.DateLastModified)
                .IsRequired();

            builder.Property(x => x.DateDeleted)
                .IsRequired(false);

            builder.Property(x => x.CreatedBy)
                .IsRequired();

            builder.Property(x => x.ModifiedBy)
                .IsRequired(false);

            builder.Property(x => x.DeletedBy)
                .IsRequired(false);
        }
  
    }
}
