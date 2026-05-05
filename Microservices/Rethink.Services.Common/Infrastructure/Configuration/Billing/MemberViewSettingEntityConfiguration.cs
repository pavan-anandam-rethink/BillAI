using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing
{
    [ExcludeFromCodeCoverage]
    public class MemberViewSettingEntityConfiguration : IEntityTypeConfiguration<MemberViewSettingEntity>
    {
        public void Configure(EntityTypeBuilder<MemberViewSettingEntity> builder)
        {
            builder.ToTable("MemberViewSetting").HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("memberId");
            //builder.HasOne(mvs => mvs.Member).WithOne(m => m.MemberViewSetting).HasForeignKey<MemberViewSettingEntity>(x => x.Id).HasConstraintName("memberId");
        }

    }
}
