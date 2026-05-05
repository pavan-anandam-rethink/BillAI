using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile
{
    public class ChildProfileReferringProviderEntityConfiguration : IEntityTypeConfiguration<ChildProfileReferringProviderEntity>
    {
        public void Configure(EntityTypeBuilder<ChildProfileReferringProviderEntity> builder)
        {
            builder.ToTable("hcChildProfileReferringProvider");
            builder.HasKey(x => x.Id);
        }
    }
}
