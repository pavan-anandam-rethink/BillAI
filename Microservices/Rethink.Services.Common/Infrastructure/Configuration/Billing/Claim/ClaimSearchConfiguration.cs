using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimSearchConfiguration : IEntityTypeConfiguration<ClaimSearchClientEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimSearchClientEntity> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }

    [ExcludeFromCodeCoverage]
    public class ClaimSearchFunderConfiguration : ClaimSearchConfiguration
    {
        public void Configure(EntityTypeBuilder<ClaimSearchFunderEntity> builder)
        {
            builder.ToTable("ClaimSearchFunders");
        }
    }

    [ExcludeFromCodeCoverage]
    public class ClaimSearchClientEntityConfiguration : IEntityTypeConfiguration<ClaimSearchClientEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimSearchClientEntity> builder)
        {
            builder.ToTable("ClaimSearchClients");
        }
    }

    [ExcludeFromCodeCoverage]
    public class ClaimSearchRenderingProviderEntityConfiguration : IEntityTypeConfiguration<ClaimSearchRenderingProviderEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimSearchRenderingProviderEntity> builder)
        {
            builder.ToTable("ClaimSearchRenderingProviders");
        }
    }

    [ExcludeFromCodeCoverage]
    public class ClaimSearchLocationEntityConfiguration : IEntityTypeConfiguration<ClaimSearchLocationEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimSearchLocationEntity> builder)
        {
            builder.ToTable("ClaimSearchLocations");
        }
    }

    [ExcludeFromCodeCoverage]
    public class ClaimSearchChildProfileAuthorizationEntityConfiguration : IEntityTypeConfiguration<ClaimSearchChildProfileAuthorizationEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimSearchChildProfileAuthorizationEntity> builder)
        {
            builder.ToTable("ClaimSearchChildProfileAuthorizations");
        }
    }
}
