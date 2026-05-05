using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim.WriteOff
{
    [ExcludeFromCodeCoverage]
    public class WriteOffApplicationConfiguration : IEntityTypeConfiguration<WriteOffApplicationEntity>
    {
        public void Configure(EntityTypeBuilder<WriteOffApplicationEntity> builder)
        {
            builder.ToTable("WriteOffApplication")
                   .HasKey(x => x.Id);

        }
    }
}
