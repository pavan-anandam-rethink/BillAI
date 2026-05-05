using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimNoteEntityConfiguration : IEntityTypeConfiguration<ClaimNoteEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimNoteEntity> builder)
        {
            builder.ToTable("ClaimNotes")
                   .HasKey(x => x.Id);
        }
    }
}