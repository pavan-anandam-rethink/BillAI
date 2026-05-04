using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.History;

[ExcludeFromCodeCoverage]
public class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("AuditLog");
        builder.HasKey(x => x.Id);

        // Required fields
        builder.Property(x => x.EntityId)
                .IsRequired();
        builder.Property(x => x.EntityName)
               .IsRequired();
        builder.Property(x => x.ActionType)
               .HasConversion<string>();
        builder.Property(x => x.OldValue)
               .IsRequired(false);
        builder.Property(x => x.NewValue)
               .IsRequired(false);
        builder.Property(x => x.ChangedBy)
               .IsRequired();
        builder.Property(x => x.AccountInfoId)
               .IsRequired();
        builder.Property(x => x.ChangedOn)
               .IsRequired();
    }
}