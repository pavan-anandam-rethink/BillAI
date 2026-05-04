using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimAttachmentConfiguration : IEntityTypeConfiguration<ClaimAttachmentEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimAttachmentEntity> builder)
        {
            builder.ToTable("ClaimAttachments");
            builder.HasKey(x => x.Id);
        }
    }
}
