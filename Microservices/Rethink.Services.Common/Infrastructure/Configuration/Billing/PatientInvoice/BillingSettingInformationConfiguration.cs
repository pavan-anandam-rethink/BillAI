using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.PatientInvoice
{    

   public class BillingSettingInformationConfiguration : IEntityTypeConfiguration<BillingSettingInformationEntity>
    {
        public void Configure(EntityTypeBuilder<BillingSettingInformationEntity> builder)
        {
            builder.ToTable("BillingSettingInformation", "dbo")
                  .HasKey(x => x.Id);

            builder.HasIndex(x => x.AccountId)
                  .IsUnique()
                  .HasDatabaseName("UQ_BillingSetting_Account");

        }
    }
}
