using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RethinkAutism.Data.Entities.Billing;

namespace RethinkAutism.Data.Configuration.Billing
{
    class FunderPreventableDateConfiguration : IEntityTypeConfiguration<FunderPreventableDateEntity>
    {
        public void Configure(EntityTypeBuilder<FunderPreventableDateEntity> builder)
        {
            builder.ToTable("hcFunderPreventableDates", schema: "dbo").HasKey(x => x.Id);

            builder.HasOne(x => x.Funder).WithMany(x => x.FunderPreventableDates).HasForeignKey(x => x.FunderId);
        }
    }
}
