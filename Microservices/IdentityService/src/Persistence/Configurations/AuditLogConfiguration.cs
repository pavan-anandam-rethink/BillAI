using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.EntityName)
            .HasMaxLength(100);

        builder.Property(e => e.EntityId)
            .HasMaxLength(100);

        builder.Property(e => e.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);

        // Indexes for query optimization
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(e => new { e.EntityName, e.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        // Global query filter for soft delete
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
