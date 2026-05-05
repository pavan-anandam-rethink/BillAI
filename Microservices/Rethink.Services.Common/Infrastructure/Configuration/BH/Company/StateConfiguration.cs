using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Company;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Company
{
    public class StateConfiguration : IEntityTypeConfiguration<StateEntity>
    {
        public void Configure(EntityTypeBuilder<StateEntity> builder)
        {
            builder.ToTable("StateLU").HasKey(x => x.Id);
        }
    }
}
