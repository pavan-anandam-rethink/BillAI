using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile
{
    public class ChildProfileContactConfiguration : IEntityTypeConfiguration<ChildProfileContactEntity>
    {
        public void Configure(EntityTypeBuilder<ChildProfileContactEntity> builder)
        {
            builder.ToTable("hcChildProfileContacts").HasKey(x => x.Id);

            //builder.HasOne(x => x.Member).WithMany(x => x.ChildProfileContacts).HasForeignKey(x => x.MemberId);
            builder.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId);
            builder.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId);
            builder.HasOne(x => x.ChildProfile).WithMany(x => x.ChildProfileContacts).HasForeignKey(x => x.ChildProfileId);
        }
    }
}
