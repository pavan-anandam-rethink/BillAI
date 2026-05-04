using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile
{
    class ChildProfileFunderMappingNoteConfiguration : IEntityTypeConfiguration<ChildProfileFunderMappingNoteEntity>
    {
        public void Configure(EntityTypeBuilder<ChildProfileFunderMappingNoteEntity> builder)
        {
            builder.ToTable("hcChildProfileFunderMappingNote").HasKey(x => x.Id);


            builder.HasOne(x => x.ChildProfileFunderMapping).WithMany(x => x.ChildProfileFunderMappingNotes).HasForeignKey(x => x.ChildProfileFunderMappingId);
        }
    }
}
