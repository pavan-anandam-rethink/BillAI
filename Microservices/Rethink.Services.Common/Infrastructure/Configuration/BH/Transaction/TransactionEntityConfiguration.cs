using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Transaction;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Transaction
{
    public class TransactionEntityConfiguration : IEntityTypeConfiguration<TransactionEntity>
    {
        public void Configure(EntityTypeBuilder<TransactionEntity> builder)
        {
            builder.ToTable("hcTransaction").HasKey(x => x.Id);

            builder.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.TransactionBy);
        }
    }
}