using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Billing;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Billing
{
    public class StaffServiceFunderConfiguraion : IEntityTypeConfiguration<StaffServiceFunderEntity>
    {
        public void Configure(EntityTypeBuilder<StaffServiceFunderEntity> builder)
        {
            builder.ToTable("hcStaffServiceFunder").HasKey(x => x.Id);

            //builder.HasOne(x => x.CreatedByMember)
            //    .WithMany()
            //    .HasForeignKey(x => x.CreatedBy);

            //builder.HasOne(x => x.StaffService)
            //    .WithMany(x => x.StaffServiceFunders)
            //    .HasForeignKey(x => x.StaffServiceId);

            //builder.HasOne(x => x.ServiceFunder)
            //    .WithMany(x => x.StaffServiceFunders)
            //    .HasForeignKey(x => x.ServiceFunderId);

        }
    }
}