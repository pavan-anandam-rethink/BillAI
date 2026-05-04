using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class CarcCodeEntityConfiguration : IEntityTypeConfiguration<CarcCodeEntity>
    {
        public void Configure(EntityTypeBuilder<CarcCodeEntity> builder)
        {
            builder.ToTable("CarcCode");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                   .HasMaxLength(10)
                   .IsUnicode(false); 

            builder.Property(x => x.Description)
                   .HasColumnType("text"); 

            builder.Property(x => x.DateDeleted);
        }
    }
}
