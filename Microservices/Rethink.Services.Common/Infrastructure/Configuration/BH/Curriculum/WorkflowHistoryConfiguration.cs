using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Curriculum;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Curriculum
{
    public class WorkflowHistoryConfiguration : IEntityTypeConfiguration<WorkflowHistoryEntity>
    {
        public void Configure(EntityTypeBuilder<WorkflowHistoryEntity> builder)
        {
            builder.ToTable("hcWorkflowHistory").HasKey(x => x.Id);
            builder.HasOne(x => x.Status).WithMany().HasForeignKey(x => new { x.TypeId, x.StatusId });

            builder.HasOne(x => x.Appointment).WithMany(x => x.WorkflowHistories).HasForeignKey(x => x.ReferenceId);
        }
    }
}
