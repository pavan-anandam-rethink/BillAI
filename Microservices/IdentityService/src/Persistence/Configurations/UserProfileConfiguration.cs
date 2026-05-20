using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.MsalObjectId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(255);

        builder.Property(e => e.FirstName)
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .HasMaxLength(100);

        builder.Property(e => e.CreatedOn)
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(e => e.MsalObjectId)
            .IsUnique()
            .HasDatabaseName("IX_UserProfiles_MsalObjectId");

        builder.HasIndex(e => e.Email)
            .HasDatabaseName("IX_UserProfiles_Email");

        builder.HasIndex(e => e.IsDeleted)
            .HasDatabaseName("IX_UserProfiles_IsDeleted");

        // Global query filter for soft delete
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Ignore computed property
        builder.Ignore(e => e.FullName);
    }
}
