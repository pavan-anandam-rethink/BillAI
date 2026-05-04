using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim.WriteOff
{
    [ExcludeFromCodeCoverage]
    public class WriteOffActionConfiguration : IEntityTypeConfiguration<WriteOffActionEntity>
    {
        public void Configure(EntityTypeBuilder<WriteOffActionEntity> builder)
        {
            builder.ToTable("WriteOffAction")
                   .HasKey(x => x.Id);

        }
    }
}
