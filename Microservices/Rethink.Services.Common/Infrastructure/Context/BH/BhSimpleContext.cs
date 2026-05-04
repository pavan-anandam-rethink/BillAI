//using System;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Rethink.Services.Common.Entities.BH.Audit;
//using Rethink.Services.Common.Entities.BH.Billing;
//using Rethink.Services.Common.Entities.BH.ChildProfile;
//using Rethink.Services.Common.Entities.BH.Company;
//using Rethink.Services.Common.Entities.BH.Curriculum;
//using Rethink.Services.Common.Entities.BH.Member;
//using Rethink.Services.Common.Entities.BH.Payment;
//using Rethink.Services.Common.Entities.BH.Propagating;
//using Rethink.Services.Common.Entities.BH.Scheduling;
//using Rethink.Services.Common.Entities.BH.Service;
//using Rethink.Services.Common.Entities.BH.Transaction;
//using Rethink.Services.Common.Infrastructure.Configuration.BH.Member;

//namespace Rethink.Services.Common.Infrastructure.Context.BH
//{
//    public class BhSimpleContext : BaseDbContext<BhSimpleContext>
//    {
//        public BhSimpleContext(DbContextOptions<BhSimpleContext> options)
//            : base(options)
//        {
//        }


//        // audit                                        
//        public virtual DbSet<ChangeAuditEntity>                            ChangeAudit                             { get; set; }
//        // billing                                                         
//        public virtual DbSet<ClearingHouseEntity>                          ClearingHouse                           { get; set; }
//        public virtual DbSet<FunderEntity>                                 hcFunder                                { get; set; }
//        public virtual DbSet<FunderInsurancePlanEntity>                    hcFunderInsurancePlans                  { get; set; }
//        public virtual DbSet<PayerEntity>                                  hcPayer                                 { get; set; }
//        public virtual DbSet<StaffServiceFunderEntity>                     hcStaffServiceFunder                    { get; set; }
//        //childprofile
//        public virtual DbSet<ChildProfileAuthorizationBillingCodeEntity>   hcChildProfileAuthorizationBillingCode  { get; set; }
//        public virtual DbSet<ChildProfileAuthorizationEntity>              hcChildProfileAuthorization             { get; set; }
//        public virtual DbSet<ChildProfileContactEntity>                    hcChildProfileContacts                  { get; set; }
//        public virtual DbSet<ChildProfileDiagnosisEntity>                  hcChildProfileDiagnosis                 { get; set; }
//        public virtual DbSet<ChildProfileFunderMappingEntity>              hcChildProfileFunderMapping             { get; set; }
//        public virtual DbSet<ChildProfileFunderServiceLineMappingEntity>   hcChildProfileFunderServiceLineMapping  { get; set; }
//        public virtual DbSet<ClientStatusEntity>                           hcClientStatus  { get; set; }


//        // company                              
//        public virtual DbSet<AddressEntity>                                hcAddress                               { get; set; }
//        public virtual DbSet<CountryEntity>                                CountryLU                               { get; set; }
//        public virtual DbSet<DiagnosisEntity>                              DiagnosisLU                             { get; set; }
//        public virtual DbSet<KareoSettingsEntity>                          KareoSettings                           { get; set; }
//        public virtual DbSet<LocationCodeEntity>                           hcLocationCodes                         { get; set; }
//        public virtual DbSet<StateEntity>                                  StateLU                                 { get; set; }
//        public virtual DbSet<TimeZoneEntity>                               hcTimezones                             { get; set; }
//        public virtual DbSet<UnitTypeEntity>                               hcUnitType                              { get; set; }
//        // curriculum                               
//        public virtual DbSet<WorkflowHistoryEntity>                        hcWorkflowHistory                       { get; set; }
//        public virtual DbSet<WorkflowStatusEntity>                         hcWorkflowStatus                        { get; set; }
//        // member                               
//        public virtual DbSet<AccountInfoEntity>                            AccountInfo                             { get; set; }
//        public virtual DbSet<ChildProfileEntity>                           ChildProfile                            { get; set; }
//        public virtual DbSet<MemberEntity>                                 tblMember                               { get; set; }
//        public virtual DbSet<PersonEntity>                                 hcPerson                                { get; set; }
//        public virtual DbSet<StaffMemberEntity>                            hcStaffMember                           { get; set; }
//        // payment                              
//        public virtual DbSet<BillingSubmissionMethodEntity>                hcBillingSubmissionMethods              { get; set; }
//        // propagating                              
//        public virtual DbSet<PropagatingAccountInfoEntity>                 hcPropagatingAccountInfo                { get; set; }
//        public virtual DbSet<PropagatingChildProfileAuthorizationEntity>   hcPropagatingChildProfileAuthorization  { get; set; }
//        public virtual DbSet<PropagatingChildProfileEntity>                hcPropagatingChildProfile               { get; set; }
//        public virtual DbSet<PropagatingChildProfileFunderEntity>          hcPropagatingChildProfileFunder         { get; set; }
//        public virtual DbSet<PropagatingFunderEntity>                      hcPropagatingFunders                    { get; set; }
//        public virtual DbSet<PropagatingProviderServiceEntity>             hcPropagatingProviderServices           { get; set; }
//        public virtual DbSet<PropagatingStaffMemberEntity>                 hcPropagatingStaffMember                { get; set; }
//        // scheduling
//        public virtual DbSet<AppointmentEntity>                            hcAppointments                          { get; set; }
//        // service                                                             
//        public virtual DbSet<ProviderBillingCodeCredentialEntity>          hcProviderBillingCodeCredentials        { get; set; }
//        public virtual DbSet<ProviderBillingCodeEntity>                    hcProviderBillingCode                   { get; set; }
//        public virtual DbSet<ProviderLocationEntity>                       hcProviderLocation                      { get; set; }
//        public virtual DbSet<ProviderServiceEntity>                        hcProviderServices                      { get; set; }
//        public virtual DbSet<ProviderServiceLineEntity>                    hcProviderServiceLine                   { get; set; }
//        public virtual DbSet<ServiceFunderEntity>                          hcServiceFunder                         { get; set; }
//        // transaction                                                          
//        public virtual DbSet<TransactionEntity>                            hcTransaction                           { get; set; }
        
//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            // audit
//            modelBuilder.Entity<ChangeAuditEntity>                            (entity => { entity.ToTable("ChangeAudit").HasKey(x => x.Id); });
//            // billing                                                         
//            modelBuilder.Entity<ClearingHouseEntity>                          (entity => { entity.ToTable("ClearingHouse").HasKey(x => x.Id); });
//            modelBuilder.Entity<FunderEntity>                                 (entity => { entity.ToTable("hcFunder").HasKey(x => x.Id); });
//            modelBuilder.Entity<FunderInsurancePlanEntity>                    (entity => { entity.ToTable("hcFunderInsurancePlans").HasKey(x => x.Id); });
//            modelBuilder.Entity<PayerEntity>                                  (entity => { entity.ToTable("hcPayer").HasKey(x => x.Id); });
//            modelBuilder.Entity<StaffServiceFunderEntity>                     (entity => { entity.ToTable("hcStaffServiceFunder").HasKey(x => x.Id); });
//            //childprofile
//            modelBuilder.Entity<ChildProfileAuthorizationBillingCodeEntity>   (entity => {
//                entity.ToTable("hcChildProfileAuthorizationBillingCode").HasKey(x => x.Id);
//                entity.HasOne(x => x.ChildProfileAuthorization).WithMany().HasForeignKey(x => x.ChildProfileAuthorizationId);
//                entity.HasOne(x => x.ProviderBillingCode).WithMany().HasForeignKey(x => x.ProviderBillingCodeId);
//            });
//            modelBuilder.Entity<ChildProfileAuthorizationEntity>              (entity => { 
//                entity.ToTable("hcChildProfileAuthorization").HasKey(x => x.Id);
//                entity.HasOne(x => x.ChildProfileDiagnosis).WithMany().HasForeignKey(x => x.ChildProfileDiagnosisId);
//            });
//            modelBuilder.Entity<ChildProfileContactEntity>                    (entity => { entity.ToTable("hcChildProfileContacts").HasKey(x => x.Id); });
//            modelBuilder.Entity<ChildProfileDiagnosisEntity>                  (entity => { 
//                entity.ToTable("hcChildProfileDiagnosis").HasKey(x => x.Id); 
//                entity.HasOne(x => x.Diagnosis).WithMany().HasForeignKey(x => x.DiagnosisId);
//            });
//            modelBuilder.Entity<ChildProfileFunderMappingEntity>              (entity => { entity.ToTable("hcChildProfileFunderMapping").HasKey(x => x.Id); });
//            modelBuilder.Entity<ChildProfileFunderServiceLineMappingEntity>   (entity => { entity.ToTable("hcChildProfileFunderServiceLineMapping").HasKey(x => x.Id); });

//            // company                              
//            modelBuilder.Entity<AddressEntity>                                (entity => { entity.ToTable("hcAddress").HasKey(x => x.Id); });
//            modelBuilder.Entity<CountryEntity>                                (entity => { entity.ToTable("CountryLU").HasKey(x => x.Id); });
//            modelBuilder.Entity<DiagnosisEntity>                              (entity => { entity.ToTable("DiagnosisLU").HasKey(x => x.Id); });
//            modelBuilder.Entity<KareoSettingsEntity>                          (entity => { entity.ToTable("KareoSettings").HasKey(x => x.Id); });
//            modelBuilder.Entity<LocationCodeEntity>                           (entity => { entity.ToTable("hcLocationCodes").HasKey(x => x.Id); });
//            modelBuilder.Entity<StateEntity>                                  (entity => { entity.ToTable("StateLU").HasKey(x => x.Id); });
//            modelBuilder.Entity<TimeZoneEntity>                               (entity => { entity.ToTable("hcTimezones").HasKey(x => x.Id); });
//            modelBuilder.Entity<UnitTypeEntity>                               (entity => { entity.ToTable("hcUnitType").HasKey(x => x.Id); });
//            // curriculum                               
//            modelBuilder.Entity<WorkflowHistoryEntity>                        (entity => { entity.ToTable("hcWorkflowHistory").HasKey(x => x.Id); });
//            modelBuilder.Entity<WorkflowStatusEntity>                         (entity => { entity.ToTable("hcWorkflowStatus").HasKey(x => x.Id); });
//            // member                               
//            modelBuilder.ApplyConfiguration(new AccountInfoEntityConfiguration());

//            modelBuilder.Entity<ChildProfileEntity>                           (entity => { entity.ToTable("ChildProfile").HasKey(x => x.Id); });
//            modelBuilder.Entity<MemberEntity>                                 (entity => { 
//                                                                                           entity.ToTable("tblMember").HasKey(x => x.Id);
//                                                                                           entity.HasOne(e => e.AccountInfo).WithMany(e => e.Members).HasForeignKey(e => e.AccountInfoId);
//            });
//            modelBuilder.Entity<PersonEntity>                                 (entity => { entity.ToTable("hcPerson").HasKey(x => x.Id); });
//            modelBuilder.Entity<StaffMemberEntity>                            (entity => { 
//                                                                                           entity.ToTable("hcStaffMember").HasKey(x => x.Id);
//                                                                                           entity.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId);
//            });

//            // payment                              
//            // propagating                              
//            modelBuilder.Entity<PropagatingAccountInfoEntity>                 (entity => { entity.ToTable("hcPropagatingAccountInfo").HasKey(x => x.Id); });
//            modelBuilder.Entity<PropagatingChildProfileAuthorizationEntity>   (entity => { entity.ToTable("hcPropagatingChildProfileAuthorization").HasKey(x => x.Id); });
//            modelBuilder.Entity<PropagatingChildProfileEntity>                (entity => { entity.ToTable("hcPropagatingChildProfile").HasKey(x => x.Id); });
//            modelBuilder.Entity<PropagatingChildProfileFunderEntity>          (entity => { entity.ToTable("hcPropagatingChildProfileFunder").HasKey(x => x.Id); });
//            modelBuilder.Entity<PropagatingFunderEntity>                      (entity => { entity.ToTable("hcPropagatingFunders").HasKey(x => x.Id); });
//            modelBuilder.Entity<PropagatingProviderServiceEntity>             (entity => { entity.ToTable("hcPropagatingProviderServices").HasKey(x => x.Id); });
//            modelBuilder.Entity<PropagatingStaffMemberEntity>                 (entity => { entity.ToTable("hcPropagatingStaffMember").HasKey(x => x.Id); });
//            // scheduling
//            modelBuilder.Entity<AppointmentEntity>                            (entity => { 
//                                                                                           entity.ToTable("hcAppointments").HasKey(x => x.Id);
//                                                                                           entity.HasOne(e => e.ModifiedByMember).WithMany().HasForeignKey(e => e.ModifiedBy);
//                                                                                           entity.HasOne(e => e.StaffMember).WithMany().HasForeignKey(e => e.StaffId);
//            });

//            // service                                                             
//            modelBuilder.Entity<ProviderBillingCodeCredentialEntity>          (entity => { entity.ToTable("hcProviderBillingCodeCredentials").HasKey(x => x.Id);
//            });
//            modelBuilder.Entity<ProviderBillingCodeEntity>                    (entity => { 
//                                                                                           entity.ToTable("hcProviderBillingCode").HasKey(x => x.Id);
//                                                                                           entity.HasOne(e => e.Funder).WithMany().HasForeignKey(e => e.FunderId); 
//                                                                                           entity.HasOne(e => e.ProviderService).WithMany().HasForeignKey(e => e.ProviderServiceId);
//                                                                                           entity.HasOne(e => e.UnitType).WithMany().HasForeignKey(e => e.UnitTypeId);
//            });
//            modelBuilder.Entity<ProviderLocationEntity>                       (entity => { entity.ToTable("hcProviderLocation").HasKey(x => x.Id); });
//            modelBuilder.Entity<ProviderServiceEntity>                        (entity => { entity.ToTable("hcProviderServices").HasKey(x => x.Id); });
//            modelBuilder.Entity<ProviderServiceLineEntity>                    (entity => { entity.ToTable("hcProviderServiceLine").HasKey(x => x.Id); });
//            modelBuilder.Entity<ServiceFunderEntity>                          (entity => { entity.ToTable("hcServiceFunder").HasKey(x => x.Id); });
//            // transaction                                                          
//            modelBuilder.Entity<TransactionEntity>                            (entity => { entity.ToTable("hcTransaction").HasKey(x => x.Id); });
//        }

//        public void Commit()
//        {
//            SaveChanges();
//        }

//        public async Task CommitAsync()
//        {
//            await SaveChangesAsync();
//        }

//        public string GetTableName(DbContext context, Type entityType)
//        {
//            throw new NotImplementedException();
//        }

//        public DbSet<TEntity> SetEntity<TEntity>() where TEntity : class
//        {
//            return Set<TEntity>();
//        }

//        public override Task LogStoredProcedureParameters(string storedProcedureName, string parameters)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}