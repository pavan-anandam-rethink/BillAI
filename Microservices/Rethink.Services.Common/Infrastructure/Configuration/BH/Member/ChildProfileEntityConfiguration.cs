using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Member
{
    public class ChildProfileEntityConfiguration : IEntityTypeConfiguration<ChildProfileEntity>
    {
        public void Configure(EntityTypeBuilder<ChildProfileEntity> builder)
        {
            builder.ToTable("ChildProfile").HasKey(x => x.Id);

            builder.HasOne(x => x.AccountInfo).WithMany().HasForeignKey(x => x.AccountInfoId);

            builder.HasOne(x => x.ProviderLocation).WithMany(x => x.ChildProfiles).HasForeignKey(x => x.FacilityId);
            builder.HasOne(x => x.StateLU).WithMany(x => x.ChildProfiles).HasForeignKey(x => x.StateId);
            builder.HasOne(x => x.CountryLU).WithMany(x => x.ChildProfiles).HasForeignKey(x => x.CountryId);

            //builder.HasOne(x => x.Grade).WithMany(x => x.ChildProfiles).HasForeignKey(x => x.GradeId);

            //builder.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId);
        }
    }
}
