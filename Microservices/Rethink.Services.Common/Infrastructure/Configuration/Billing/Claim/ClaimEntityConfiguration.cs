using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimEntityConfiguration : IEntityTypeConfiguration<ClaimEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimEntity> builder)
        {
            builder.ToTable("Claims");
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.ProviderLocation);
            //builder.Ignore(x => x.ModifiedByMember);
            builder.Ignore(x => x.AccountInfo);
            builder.Ignore(x => x.RenderingStaffMember);
            //builder.Ignore(x => x.Member);
            builder.Ignore(x => x.ClientFunder);
            builder.Ignore(x => x.ChildProfile);
            //builder.Ignore(x => x.PrimaryFunder);
            //builder.Ignore(x => x.SecondaryFunder);
            //builder.Ignore(x => x.TertiaryFunder);
            builder.Ignore(x => x.LocationCode);
        }
    }
}
