using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile
{
    public class ReferringProviderEntityConfiguration : IEntityTypeConfiguration<ReferringProviderEntity>
    {
        public void Configure(EntityTypeBuilder<ReferringProviderEntity> builder)
        {
            builder.ToTable("hcReferringProvider");
            builder.HasKey(x => x.Id);
        }
    }
}
