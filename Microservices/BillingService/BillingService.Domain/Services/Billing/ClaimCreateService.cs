using BillingService.Domain.Extensions;
using BillingService.Domain.Interfaces.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Reporting;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ClaimCreateService : BaseService, IClaimCreateService
    {
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly ILogger<ClaimCreateService> _logger;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;

        private readonly IRepository<BillingDbContext, ClaimAppointmentLinkEntity> _claimAppointmentLinkRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchFunderEntity> _claimsSearchFundersRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchClientEntity> _claimsSearchClientsRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity> _claimSearchRenderingProvidersRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchLocationEntity> _claimsSearchLocationsRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchChildProfileAuthorizationEntity> _claimSearchAuthsRepository;
        private readonly IRepository<ReportingDbContext, ClientsEntity> _clientNameReportingRepository;
        private readonly IRepository<ReportingDbContext, FundersEntity> _funderNameReportingRepository;

        private readonly IClaimUpdateService _claimUpdateService;


        public ClaimCreateService(
            ILogger<ClaimCreateService> logger,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, ClaimSearchFunderEntity> claimsSearchFundersRepository,
            IRepository<BillingDbContext, ClaimSearchClientEntity> claimsSearchClientsRepository,
            IRepository<BillingDbContext, ClaimAppointmentLinkEntity> claimAppointmentLinkRepository,
            IRepository<BillingDbContext, ClaimSearchLocationEntity> claimsSearchLocationsRepository,
            IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity> claimSearchRenderingProvidersRepository,
            IRepository<BillingDbContext, ClaimSearchChildProfileAuthorizationEntity> claimSearchAuthsRepository,
            IRethinkMasterDataMicroServices rethinkServices,
            IRepository<ReportingDbContext, ClientsEntity> clientNameReportingRepository,
            IRepository<ReportingDbContext, FundersEntity> funderNameReportingRepository,
            IClaimUpdateService claimUpdateService)
        {
            _funderNameReportingRepository = funderNameReportingRepository;
            _clientNameReportingRepository = clientNameReportingRepository;
            _logger = logger;
            _claimRepository = claimRepository;
            _claimsSearchFundersRepository = claimsSearchFundersRepository;
            _claimsSearchClientsRepository = claimsSearchClientsRepository;
            _claimAppointmentLinkRepository = claimAppointmentLinkRepository;
            _claimsSearchLocationsRepository = claimsSearchLocationsRepository;
            _claimSearchRenderingProvidersRepository = claimSearchRenderingProvidersRepository;
            _claimSearchAuthsRepository = claimSearchAuthsRepository;
            _rethinkServices = rethinkServices;
            _claimUpdateService = claimUpdateService;
        }

        public async Task ProcessClaimCreation(ClaimCreateEnd model)
        {
            _logger.LogInformation($"Processing claim: #{model.ClaimId} creation result");

            try
            {
                if (model.FunderId != null)
                {
                    await ImportSearchFunder(model.AccountInfoId, (int)model.FunderId);
                }

                if (model.ClientId != null)
                {
                    await ImportClientSearch(model.AccountInfoId, (int)model.ClientId);
                }

                if (model.RenderingProviderTypeId != null || model.RenderingProviderId != null)
                {
                    await ImportRenderingProviderSearch(model.AccountInfoId, model.ClaimId, (int)model.RenderingProviderTypeId, (int)model.RenderingProviderId);
                }

                if (model.ChildProfileAuthorizationId != null)
                {
                    await ImportChildProfileAuthorization(model.AccountInfoId, (int)model.ClientId, (int)model.ChildProfileAuthorizationId);
                }

                await ImportSecondaryFunderDetails(model.AccountInfoId, model.ClaimId);
                await MigrateLocationsToBilling();

                _logger.LogInformation($"Processed claim: #{model.ClaimId}");


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing claim: #{model.ClaimId} submission result " +
                    $"Error: {ex.Message}");
                throw;
            }
        }

        private async Task ImportSearchFunder(int accountInfoId, int funderId)
        {
            var bhFunder = await _rethinkServices.GetFunder(accountInfoId, funderId);
            if (bhFunder != null)
            {
                var existingClaimSearchFunder = await _claimsSearchFundersRepository.Query().Where(x => x.Id == funderId).FirstOrDefaultAsync();
                var existingFunder = await _funderNameReportingRepository.Query().Where(x => x.FunderId == funderId).FirstOrDefaultAsync();
                if (existingClaimSearchFunder != null)
                {
                    existingClaimSearchFunder.DateDeleted = bhFunder.metaData.deletedOn;
                    existingClaimSearchFunder.Name = bhFunder.funderName;
                    _claimsSearchFundersRepository.Update(existingClaimSearchFunder);
                }
                else
                {
                    await _claimsSearchFundersRepository.AddAsync(new ClaimSearchFunderEntity { Id = bhFunder.id, Name = bhFunder.funderName, DateDeleted = bhFunder.metaData.deletedOn });
                }
                if (existingFunder != null)
                {
                    existingFunder.DateDeleted = bhFunder.metaData.deletedOn;
                    existingFunder.FunderName = bhFunder.funderName;
                    existingFunder.DateModified = EstDateTime;
                    _funderNameReportingRepository.Update(existingFunder);
                }
                else
                {
                    await _funderNameReportingRepository.AddAsync(new FundersEntity { FunderId = bhFunder.id, FunderName = bhFunder.funderName, DateCreated = bhFunder.metaData.createdOn, DateModified = bhFunder.metaData.modifiedOn, DateDeleted = bhFunder.metaData.deletedOn });
                }
                await _funderNameReportingRepository.CommitAsync();
                await _claimsSearchFundersRepository.CommitAsync();
            }
            else
            {
                throw new Exception("BH Funder not found");
            }
        }

        private async Task ImportClientSearch(int accountInfoId, int clientId)
        {
            var bhPatient = await _rethinkServices.GetChildProfile(accountInfoId, clientId);

            if (bhPatient != null)
            {
                var existingClaimsPatient = await _claimsSearchClientsRepository.Query()
                .Where(x => x.Id == clientId).FirstOrDefaultAsync();

                var existingClient = await _clientNameReportingRepository.Query().Where(x => x.ClientId == clientId).FirstOrDefaultAsync();

                if (existingClaimsPatient != null)
                {
                    if (bhPatient.name.middleName != existingClaimsPatient.middleName || bhPatient.name.lastName != existingClaimsPatient.lastName || bhPatient.name.firstName != existingClaimsPatient.firstName || bhPatient.metaData.deletedOn != existingClaimsPatient.DateDeleted)
                    {
                        existingClaimsPatient.lastName = bhPatient.name.lastName;
                        existingClaimsPatient.firstName = bhPatient.name.firstName;
                        existingClaimsPatient.middleName = bhPatient.name.middleName;
                        existingClaimsPatient.DateDeleted = bhPatient.metaData.deletedOn;
                        _claimsSearchClientsRepository.Update(existingClaimsPatient);
                    }
                }
                else
                {
                    var addPatient = new ClaimSearchClientEntity
                    {
                        Id = bhPatient.id,
                        lastName = bhPatient.name.lastName,
                        firstName = bhPatient.name.firstName,
                        middleName = bhPatient.name.middleName,
                        DateDeleted = bhPatient.metaData.deletedOn
                    };
                    await _claimsSearchClientsRepository.AddAsync(addPatient);
                }
                if (existingClient != null)
                {
                    if (bhPatient.name.middleName != existingClient.ClientMiddleName || bhPatient.name.lastName != existingClient.ClientLastName || bhPatient.name.firstName != existingClient.ClientFirstName || bhPatient.metaData.deletedOn != existingClient.DateDeleted)
                    {
                        existingClient.ClientLastName = bhPatient.name.lastName;
                        existingClient.ClientFirstName = bhPatient.name.firstName;
                        existingClient.ClientMiddleName = bhPatient.name.middleName;
                        existingClient.DateDeleted = bhPatient.metaData.deletedOn;
                        _clientNameReportingRepository.Update(existingClient);
                    }
                }
                else
                {
                    var addPatient = new ClientsEntity
                    {
                        ClientId = bhPatient.id,
                        ClientLastName = bhPatient.name.lastName,
                        ClientFirstName = bhPatient.name.firstName,
                        ClientMiddleName = bhPatient.name.middleName,
                        DateCreated = bhPatient.metaData.createdOn,
                        DateModified = bhPatient.metaData.modifiedOn,
                        DateDeleted = bhPatient.metaData.deletedOn
                    };
                    await _clientNameReportingRepository.AddAsync(addPatient);
                }
                await _clientNameReportingRepository.CommitAsync();
                await _claimsSearchClientsRepository.CommitAsync();
            }

            else
            {
                throw new Exception("BH Patient not found");
            }
        }

        private async Task ImportSecondaryFunderDetails(int accountInfoId, int claimId)
        {
            var claim = _claimRepository.Query().FirstOrDefault(x => x.Id == claimId && x.DateDeleted == null);
            claim.IsSecondaryPayerAvailable = false;
            var secondaryFunderDetails = await _claimUpdateService.CheckAndGetSecondaryFunderDetails(accountInfoId, claim);
            if (secondaryFunderDetails != null && secondaryFunderDetails.funders.Any())
            {
                claim.IsSecondaryPayerAvailable = true;
            }
            MarkUpdated(claim, claim.MemberId);
            _claimRepository.Update(claim);
            await _claimRepository.CommitAsync();
        }

        private async Task ImportRenderingProviderSearch(int accountInfoId, int claimId, int renderingProviderTypeId, int renderingProviderId)
        {
            ClaimSearchRenderingProviderEntity bhClaimSearchRenderingProvider = null;
            if (renderingProviderTypeId > 0 || renderingProviderId > 0)
            {
                RethinkStaffMember bhRenderingProvider = null;
                bhRenderingProvider = await _rethinkServices.GetStaffMember(accountInfoId, renderingProviderTypeId);
                if ((renderingProviderTypeId > 0 && bhRenderingProvider.Member == null))
                {
                    var renderingProviders = await _rethinkServices.GetRenderingProvidersAsync(accountInfoId, true);
                    renderingProviderTypeId = renderingProviders.FirstOrDefault(x => x.StaffMemberId == renderingProviderId).Id;
                    bhRenderingProvider = await _rethinkServices.GetStaffMember(accountInfoId, renderingProviderTypeId);
                }
                if (bhRenderingProvider.Member == null)
                {
                    bhClaimSearchRenderingProvider = new ClaimSearchRenderingProviderEntity
                    {
                        Id = bhRenderingProvider.memberId,
                        Name = FullNameExt.GetFullName(bhRenderingProvider.name.firstName, bhRenderingProvider.name.middleName, bhRenderingProvider.name.lastName),
                        DateDeleted = bhRenderingProvider.metaData.deletedOn
                    };
                }
            }
            else
            {
                var claim = await _claimRepository.Query()
                    .Include(x => x.ClaimSubmissions)
                    .Where(x => x.Id == claimId).FirstOrDefaultAsync();
                var appLink = await _claimAppointmentLinkRepository.Query()
                    .Where(ca => ca.ClaimId == claim.Id).FirstOrDefaultAsync();
                var bhAuth = await _rethinkServices.GetChildProfileAuthorizationById(claim.AccountInfoId, (int)claim.AuthorizationId);

                renderingProviderTypeId = (int)(bhAuth?.authorizationRenderingProviderTypeId);
                var bhRenderingProviderId = bhAuth?.renderingProviderStaffId ?? claim.RenderingStaffMemberId;
                var rendringProviderName = string.Empty;
                DateTime? dateDeleted = null;

                if (appLink != null && renderingProviderTypeId == (int)AuthorizationRenderingProviderTypes.ProviderAssignedToAppointment)
                {
                    var appId = appLink.AppointmentId;
                    var app = await _rethinkServices.GetAppointmentAsync(appId);

                    if (app != null)
                    {
                        app.StaffMember = await _rethinkServices.GetStaffMember(accountInfoId, app.staffId);
                        app.StaffMember.Member = await _rethinkServices.GetMemberAsync(accountInfoId, app.StaffMember.memberId);
                        bhRenderingProviderId = app.StaffMember.memberId;
                        rendringProviderName = FullNameExt.GetFullName(app.StaffMember.Member.firstName,
                                                               app.StaffMember.Member.middleName,
                                                               app.StaffMember.Member.lastName);
                        dateDeleted = app.StaffMember.Member.metaData.deletedOn;
                    }
                }
                else
                {
                    var accountInfo = await _rethinkServices.GetAccountReturningEntityAsync(claim.AccountInfoId);
                    rendringProviderName = accountInfo.AccountOrganizationName;
                    dateDeleted = accountInfo.DateDeleted;
                }

                if (bhRenderingProviderId != null)
                {
                    bhClaimSearchRenderingProvider = new ClaimSearchRenderingProviderEntity { Id = (int)bhRenderingProviderId, Name = rendringProviderName, DateDeleted = dateDeleted };
                }
            }

            if (bhClaimSearchRenderingProvider != null)
            {
                var existingRP = await _claimSearchRenderingProvidersRepository.Query()
                    .Where(x => x.Id == bhClaimSearchRenderingProvider.Id).FirstOrDefaultAsync();

                if (existingRP != null)
                {
                    if (bhClaimSearchRenderingProvider.Name != existingRP.Name || bhClaimSearchRenderingProvider.DateDeleted != existingRP.DateDeleted)
                    {
                        existingRP.Name = bhClaimSearchRenderingProvider.Name;
                        existingRP.DateDeleted = bhClaimSearchRenderingProvider.DateDeleted;
                        _claimSearchRenderingProvidersRepository.Update(existingRP);
                    }
                }
                else
                {
                    await _claimSearchRenderingProvidersRepository.AddAsync(bhClaimSearchRenderingProvider);
                }

                await _claimSearchRenderingProvidersRepository.CommitAsync();
            }
            else
            {
                throw new Exception("BH RenderingProvider not found");
            }
        }

        private async Task MigrateLocationsToBilling()
        {
            var locationCodes = await _rethinkServices.GetLocationCodes();

            if (locationCodes.Any())
            {
                var existingLocations = locationCodes.Select(x => new ClaimSearchLocationEntity { Id = x.id }).ToList();
                await _claimsSearchLocationsRepository.BulkReadContainsAsync(existingLocations);

                var existingLocationsIds = existingLocations.Select(x => x.Id).ToList();
                var createLocations = locationCodes.Where(x => !existingLocationsIds.Contains(x.id)).ToList();

                var updated = false;
                foreach (var item in existingLocations)
                {
                    var sourceLocation = locationCodes.First(x => x.id == item.Id);
                    if (sourceLocation.description != item.Name)
                    {
                        item.Name = sourceLocation.description;
                        updated = true;
                    }
                }

                var newLocations = createLocations.Select(x => new ClaimSearchLocationEntity { Id = x.id, Name = x.description });
                await _claimsSearchLocationsRepository.AddRangeAsync(newLocations);

                if (newLocations.Any() || updated)
                {
                    await _claimsSearchLocationsRepository.CommitAsync();
                }
            }
        }

        private async Task ImportChildProfileAuthorization(int accountInfoId, int clientId, int childProfileAuthorizationId)
        {
            var bhAuth = await _rethinkServices.GetChildProfileAuthorizationByClientId(accountInfoId, clientId, childProfileAuthorizationId);
            bhAuth.ChildProfileFunderServiceLineMapping = await
             _rethinkServices.GetChildProfileFunderServiceLineMappingDataByClient(accountInfoId, clientId, bhAuth.funderId, bhAuth.providerServiceId);

            var x = new ClaimSearchChildProfileAuthorizationEntity
            {
                ChildProfileFunderId = bhAuth.ChildProfileFunderServiceLineMapping.ChildProfileFunderMappingId,
                Id = bhAuth.id,
                //FunderId = bhAuth.Funder.id,
                FunderId = bhAuth.funderId,
                DateDeleted = bhAuth.metaData.deletedOn
            };

            if (bhAuth != null)
            {
                var existingAuth = await _claimSearchAuthsRepository.Query()
                    .Where(x => x.Id == childProfileAuthorizationId)
                    .FirstOrDefaultAsync();
                if (existingAuth != null)
                {
                    if (existingAuth.ChildProfileFunderId != x.ChildProfileFunderId ||
                        existingAuth.DateDeleted != x.DateDeleted ||
                        existingAuth.FunderId != x.FunderId)
                    {
                        existingAuth.ChildProfileFunderId = x.ChildProfileFunderId;
                        existingAuth.DateDeleted = x.DateDeleted;
                        existingAuth.FunderId = x.FunderId;

                        _claimSearchAuthsRepository.Update(existingAuth);
                    }
                }
                else
                {
                    await _claimSearchAuthsRepository.AddAsync(x);
                }

                await _claimSearchAuthsRepository.CommitAsync();
            }
            else
            {
                throw new Exception("BH ChildProfileAuthorization not found");
            }
        }
    }
}
