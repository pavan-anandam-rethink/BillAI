using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim.WriteOff
{
    [ExcludeFromCodeCoverage]
    public class WriteOffReasonCodeConfiguration : IEntityTypeConfiguration<WriteOffReasonCodeEntity>
    {
        public void Configure(EntityTypeBuilder<WriteOffReasonCodeEntity> builder)
        {
            builder.ToTable("WriteOffReasonCode")
                   .HasKey(x => x.Id);

        }
    }
}
