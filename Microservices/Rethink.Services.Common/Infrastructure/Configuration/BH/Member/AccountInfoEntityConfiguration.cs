using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Member
{
    public class AccountInfoEntityConfiguration : IEntityTypeConfiguration<AccountInfoEntity>
    {
        public void Configure(EntityTypeBuilder<AccountInfoEntity> builder)
        {
            builder.ToTable("AccountInfo").HasKey(x => x.Id);

            //builder.Ignore(x => x.TimeZone);
            //builder.HasOne(x => x.TimeZone)
            //    .WithMany()
            //    .HasForeignKey(x => x.HcTimezoneId);
            builder.HasOne(x => x.ClearingHouse).WithMany().HasForeignKey(x => x.ClearingHouseId);
            builder.HasOne(x => x.StateLU).WithMany().HasForeignKey(x => x.BillingStateId);
            // builder.HasOne(x => x.CountryLU).WithMany().HasForeignKey(x => x.BillingCountryId);
            // builder.HasOne(x => x.LearningProcessAttribute).WithMany(x => x.AccountInfos).HasForeignKey(x => x.LearningProcessAttributesId);
        }
    }
}
