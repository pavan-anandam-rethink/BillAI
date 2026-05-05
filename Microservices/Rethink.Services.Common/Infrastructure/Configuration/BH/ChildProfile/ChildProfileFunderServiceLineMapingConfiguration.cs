using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;
using Rethink.Services.Common.Enums.BH;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile
{
    class ChildProfileFunderServiceLineMapingConfiguration : IEntityTypeConfiguration<ChildProfileFunderServiceLineMappingEntity>
    {
        public void Configure(EntityTypeBuilder<ChildProfileFunderServiceLineMappingEntity> builder)
        {
            builder.ToTable("hcChildProfileFunderServiceLineMapping")
                   .HasKey(x => x.Id);

            //builder.Property(x => x.ResponsibilitySequence)
            //     .HasConversion<string>();
            //builder.Property<ResponsibilitySequenceType>(x => x.ResponsibilitySequence)
            //     .HasConversion<char>(x => (char)x,
            //         x => (ResponsibilitySequenceType)x);

            builder.HasOne(x => x.ChildProfileFunderMapping)
                   .WithMany(x => x.ChildProfileFunderServiceLineMapings)
                   .HasForeignKey(x => x.ChildProfileFunderMappingId);
            builder.HasOne(x => x.ProviderSeviceLine)
                   .WithMany(x=>x.ChildProfileFunderServiceLineMapings)
                   .HasForeignKey(x => x.ServiceId);
        }
    }
}
