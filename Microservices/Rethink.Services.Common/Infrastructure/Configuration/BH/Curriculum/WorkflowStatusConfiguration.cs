using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Curriculum;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Curriculum
{
    public class WorkflowStatusConfiguration : IEntityTypeConfiguration<WorkflowStatusEntity>
    {
        public void Configure(EntityTypeBuilder<WorkflowStatusEntity> builder)
        {
            builder.ToTable("hcWorkflowStatus").HasKey(x => new { x.Id, x.StatusId});
           
        }
    }
}
