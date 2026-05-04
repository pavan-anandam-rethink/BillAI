using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Member
{
    public class StaffStatusConfiguration : IEntityTypeConfiguration<StaffStatusEntity>
    {
        public void Configure(EntityTypeBuilder<StaffStatusEntity> builder)
        {
            builder.ToTable("hcStaffStatus").HasKey(x => x.Id);
        }
    }
}