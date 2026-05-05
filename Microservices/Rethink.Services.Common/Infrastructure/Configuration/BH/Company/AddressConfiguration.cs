using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Company;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Company
{
    public class AddressConfiguration : IEntityTypeConfiguration<AddressEntity>
    {
        public void Configure(EntityTypeBuilder<AddressEntity> builder)
        {
            builder.ToTable("Address").HasKey(x => x.Id);

            builder.HasOne(x => x.StateLU).WithMany().HasForeignKey(x => x.StateId);
            builder.HasOne(x => x.CountryLU).WithMany().HasForeignKey(x => x.CountryId);
        }
    }
}
