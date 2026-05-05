using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    public class StateEntityConfiguration : IEntityTypeConfiguration<StateEntity>
    {
        public void Configure(EntityTypeBuilder<StateEntity> builder)
        {
            // Map to table
            builder.ToTable("State");

            // Primary key
            builder.HasKey(x => x.Id);           // EF primary key
            builder.Property(x => x.Id)
                   .HasColumnName("StateId");

            // StateName
            builder.Property(x => x.StateName)
                   .HasMaxLength(31)
                   .IsRequired();

            // StateCode
            builder.Property(x => x.StateCode)
                   .HasMaxLength(2)
                   .IsRequired();

            // UTC offsets
            builder.Property(x => x.UtcOffSet)
                   .IsRequired(false);

            builder.Property(x => x.UtcDSTOffSet)
                   .IsRequired(false);

            // Sandata support
            builder.Property(x => x.SupportsSandata)
                   .IsRequired();

            // Auditing fields
            builder.Property(x => x.DateCreated)
                   .IsRequired()
                   .HasColumnType("datetime");

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
        }
    }
}
