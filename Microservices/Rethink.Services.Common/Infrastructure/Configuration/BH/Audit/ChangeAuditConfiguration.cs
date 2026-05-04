using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Audit;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Audit
{
    public class ChangeAuditConfiguration : IEntityTypeConfiguration<ChangeAuditEntity>
    {
        public void Configure(EntityTypeBuilder<ChangeAuditEntity> builder)
        {
            builder.ToTable("ChangeAudit").HasKey(x => x.Id);
        }
    }
}