using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Reporting;
using Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Infrastructure.Context.Billing
{
    [ExcludeFromCodeCoverage]
    public class ReportingDbContext : BaseDbContext<ReportingDbContext>
    {

        public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AccountsReceivableEntity> ArReportingEntities { get; set; }
        public virtual DbSet<PaymentsAdjustmentsEntity> PayAdjReportingEntities { get; set; }
        public virtual DbSet<ClaimStatusEntity> ClaimStatusReportingEntities { get; set; }
        public virtual DbSet<ClientsEntity> ClientNameReportingEntities { get; set; }
        public virtual DbSet<FundersEntity> FunderNameReportingEntities { get; set; }


        public override Task LogStoredProcedureParameters(string storedProcedureName, string parameters)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Reporting tables configuration
            modelBuilder.ApplyConfiguration(new AccountsReceivableEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentsAdjustmentsEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ClaimStatusEntityConfigurator());
            modelBuilder.ApplyConfiguration(new ClientsEntityConfigurator());
            modelBuilder.ApplyConfiguration(new FundersEntityConfigurator());

            base.OnModelCreating(modelBuilder);

        }

    }
}