using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile
{
    public class ChildProfileFunderMappingEntityConfiguration : IEntityTypeConfiguration<ChildProfileFunderMappingEntity>
    {
        public void Configure(EntityTypeBuilder<ChildProfileFunderMappingEntity> builder)
        {
            builder.ToTable("hcChildProfileFunderMapping").HasKey(x => x.Id);
            builder.HasOne(x => x.Funder).WithMany().HasForeignKey(x => x.FunderId);
            builder.HasOne(x => x.InsuranceContact).WithMany(x => x.ChildProfileFunderMapping).HasForeignKey(x => x.ChildProfileInsuranceContact);
            builder.HasOne(x => x.ChildProfile).WithMany().HasForeignKey(x => x.ChildProfileId);
            //builder.HasOne(x => x.FunderCaseManager).WithMany().HasForeignKey(x => x.FunderCaseManagerId);
        }
    }
}
