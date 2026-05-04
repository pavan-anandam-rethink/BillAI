using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Member
{
    public class PersonConfiguraion : IEntityTypeConfiguration<PersonEntity>
    {
        public void Configure(EntityTypeBuilder<PersonEntity> builder)
        {
            builder.ToTable("hcPerson").HasKey(x => x.Id);
        }
    }
}