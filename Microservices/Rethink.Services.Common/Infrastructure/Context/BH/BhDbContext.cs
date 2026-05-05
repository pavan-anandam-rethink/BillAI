//using Microsoft.EntityFrameworkCore;
//using Rethink.Services.Common.Entities.BH.Billing;
//using Rethink.Services.Common.Entities.BH.ChildProfile;
//using Rethink.Services.Common.Entities.BH.Company;
//using Rethink.Services.Common.Entities.BH.Member;
//using Rethink.Services.Common.Entities.BH.Propagating;
//using Rethink.Services.Common.Entities.BH.Service;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Audit;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Billing;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Company;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Curriculum;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Member;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Payment;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Propagating;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Scheduling;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Service;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Transaction;
//using RethinkAutism.Data.Configuration.Billing;
//using System.Threading.Tasks;

//namespace Rethink.Services.Common.Infrastructure.Context.BH
//{
//    public class BhDbContext : BaseDbContext<BhDbContext>
//    {
//        public BhDbContext(DbContextOptions<BhDbContext> options)
//            : base(options)
//        {
//        }

//        public virtual DbSet<AccountInfoEntity>               AccountInfo { get; set; }
//        public virtual DbSet<ChildProfileEntity>              ChildProfile { get; set; }
//        public virtual DbSet<ProviderLocationEntity>          hcProviderLocation { get; set; }
//        public virtual DbSet<LocationCodeEntity>              hcLocationCode { get; set; }
//        public virtual DbSet<PlaceOfServiceEntity>            hcPlaceOfService { get; set; }
//        //public virtual DbSet<ChildProfileAuthorizationEntity> hcChildProfileAuthorization { get; set; }
//        public virtual DbSet<StaffMemberEntity>               hcStaffMember { get; set; }
//        public virtual DbSet<ReferringProviderEntity>         hcReferringProvider { get; set; }
//        public virtual DbSet<ClearingHouseEntity>             ClearingHouse { get; set; }
//        public virtual DbSet<FunderEntity>                    hcFunder { get; set; }
//        public virtual DbSet<AddressEntity>                   hcAddress { get; set; }
//        public virtual DbSet<StateEntity>                     StateLU { get; set; }
//        public virtual DbSet<CountryEntity>                   CountryLU { get; set; }
//        public virtual DbSet<DiagnosisEntity>                 DiagnosisLU { get; set; }
//        public virtual DbSet<PropagatingStaffMemberEntity>    hcPropagatingStaffMember { get; set; }

//        public virtual DbSet<MemberEntity>                    hcMembers { get; set; }
        
//        public virtual DbSet<KareoSettingsEntity>             kareoSettings { get; set; }
//        public virtual DbSet<ClientStatusEntity> ClientStatus { get; set; }

//        public override Task LogStoredProcedureParameters(string storedProcedureName, string parameters)
//        {
//            throw new System.NotImplementedException();
//        }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            modelBuilder.ApplyConfiguration(new AccountInfoEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new DiagnosisEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new FunderEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new AppointmentEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new UnitTypeEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new ClearingHouseEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new PayerConfiguration());
//            modelBuilder.ApplyConfiguration(new TimeZoneEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new ChargePaymentEntityConfiguration());

//            modelBuilder.ApplyConfiguration(new ChildProfileEntityConfiguration());
//            //modelBuilder.ApplyConfiguration(new ChildProfileAuthorizationConfiguration());
//            modelBuilder.ApplyConfiguration(new FunderPreventableDateConfiguration());
//            //modelBuilder.ApplyConfiguration(new ChildProfileAuthorizationBillingCodeConfiguration());
//            //modelBuilder.ApplyConfiguration(new ChildProfileAuthorizationDiagnosisCodeConfiguration());
//            modelBuilder.ApplyConfiguration(new ChildProfileFunderServiceLineMapingConfiguration());
//            modelBuilder.ApplyConfiguration(new ChildProfileReferringProviderEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new ChildProfileDiagnosisEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new ChildProfileFunderMappingEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new ChildProfileContactConfiguration());
//            modelBuilder.ApplyConfiguration(new ChildProfileFunderMappingNoteConfiguration());

//            modelBuilder.ApplyConfiguration(new ProviderLocationConfiguration());
//            modelBuilder.ApplyConfiguration(new ProviderBillingCodeEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new ProviderServiceEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new ProviderServiceLineConfiguration());
//            modelBuilder.ApplyConfiguration(new ProviderBillingCodeCredentialEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new ServiceFunderConfiguration());
//            modelBuilder.ApplyConfiguration(new LocationCodeEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new StaffMemberConfiguraion());
//            modelBuilder.ApplyConfiguration(new StaffTitleConfiguration());
//            modelBuilder.ApplyConfiguration(new StaffStatusConfiguration());
//            modelBuilder.ApplyConfiguration(new AccountRoleConfiguration());
//            modelBuilder.ApplyConfiguration(new ReferringProviderEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new ReasonCodeEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new StaffServiceFunderConfiguraion());
//            modelBuilder.ApplyConfiguration(new ChangeAuditConfiguration());
//            modelBuilder.ApplyConfiguration(new PlaceOfServiceConfiguration());
//            modelBuilder.ApplyConfiguration(new ClientStatusEntityConfiguration());

//            modelBuilder.ApplyConfiguration(new AddressConfiguration());
//            modelBuilder.ApplyConfiguration(new CountryEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new StateConfiguration());
//            modelBuilder.Entity<FunderInsurancePlanEntity>(entity => {
//                entity.ToTable("hcFunderInsurancePlans").HasKey(x => x.Id);
//                entity.HasOne(x => x.Funder).WithMany(x => x.FunderInsurancePlans).HasForeignKey(x => x.FunderId);
//            });

//            modelBuilder.ApplyConfiguration(new MemberEntityConfiguration());
//            modelBuilder.ApplyConfiguration(new PersonConfiguraion());

//            modelBuilder.ApplyConfiguration(new PropagatingAccountInfoConfiguration());
//            modelBuilder.ApplyConfiguration(new PropagatingProviderServiceConfiguration());
//            modelBuilder.ApplyConfiguration(new PropagatingChildProfileConfiguration());
//            modelBuilder.ApplyConfiguration(new PropagatingStaffMemberConfiguration());
//            modelBuilder.ApplyConfiguration(new PropagatingChildProfileAuthorizationConfiguration());
//            modelBuilder.ApplyConfiguration(new PropagatingFunderConfiguration());
//            modelBuilder.ApplyConfiguration(new PropagatingChildProfileFunderConfiguration());

//            modelBuilder.ApplyConfiguration(new WorkflowHistoryConfiguration());
//            modelBuilder.ApplyConfiguration(new WorkflowStatusConfiguration());
//            modelBuilder.ApplyConfiguration(new TransactionEntityConfiguration());

//            modelBuilder.Entity<KareoSettingsEntity>(entity => { entity.ToTable("KareoSettings").HasKey(x => x.Id); });
//        }

//    }
//}
