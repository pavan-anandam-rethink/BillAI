using AutoMapper;
using Azure.Storage.Blobs.Models;
using BillingService.Domain.Extensions;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.BillingSettings;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.BillingSettings;
using BillingService.Domain.Services.Billing;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Feature;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.BillingSetting
{
    public class BillingSettingsService : BaseService, IBillingSettingsService
    {
        private readonly IRepository<BillingDbContext, ClaimFilingIndicatorEntity> _claimFilingIndicatorRepo;
        private readonly IRepository<BillingDbContext, FunderSettingsEntity> _funderSettingRepo;
        private readonly IRepository<BillingDbContext, FeatureEntity> _featureRepository;
        private readonly IRepository<BillingDbContext, TimezonesEntity> _timezonesEntity;
        private readonly IRepository<BillingDbContext, AccountFeatureSettingEntity> _accountFeatureSettingRepository;
        private readonly IRepository<BillingDbContext, BillingSettingInformationEntity> _billingSettingInformationRepository;
        private readonly ILogger<BillingSettingsService> _logger;
        private readonly BillingDbContext _context;
        private IMapper _mapper;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly ICacheService _cacheService;
        private const string statesCacheKey = "allStates";          // Cache key for GetStatesAsync method.
        private static readonly TimeSpan statesCacheDuration = TimeSpan.FromHours(24); // 24 hours
        private readonly IRepository<BillingDbContext, StateEntity> _stateRepository;

        /// <summary>
        /// Initializes a new instance of the BillingSettingsService class, which manages billing settings and related
        /// operations.
        /// </summary>
        /// <remarks>This constructor aggregates repositories, a logger, and a mapper to provide a
        /// cohesive interface for managing billing settings. Use this service to coordinate billing-related data access
        /// and logging.</remarks>
        /// <param name="claimFilingIndicatorRepo">The repository used to access and manage claim filing indicator entities within the billing database
        /// context.</param>
        /// <param name="funderSettingRepo">The repository used to access and manage funder settings entities within the billing database context.</param>
        /// <param name="logger">The logger used for recording informational and error messages related to billing settings operations.</param>
        /// <param name="mapper">The mapper used to transform objects between different types, facilitating data transfer and conversion.</param>
        public BillingSettingsService(
            IRepository<BillingDbContext, ClaimFilingIndicatorEntity> claimFilingIndicatorRepo,
            IRepository<BillingDbContext, FunderSettingsEntity> funderSettingRepo,
            IRepository<BillingDbContext, FeatureEntity> featureRepository,
            IRepository<BillingDbContext, AccountFeatureSettingEntity> accountFeatureSettingRepository,
            IRepository<BillingDbContext, BillingSettingInformationEntity> billingSettingInformationRepository,
            ILogger<BillingSettingsService> logger,
            IMapper mapper,
            BillingDbContext context,
            IRethinkMasterDataMicroServices rethinkServices,
            IRepository<BillingDbContext, TimezonesEntity> timezonesEntity,
            ICacheService cacheService,
            IRepository<BillingDbContext, StateEntity>  stateRepository)
        {
            _claimFilingIndicatorRepo = claimFilingIndicatorRepo;
            _funderSettingRepo = funderSettingRepo;
            _featureRepository = featureRepository;
            _accountFeatureSettingRepository = accountFeatureSettingRepository;
            _billingSettingInformationRepository = billingSettingInformationRepository;
            _logger = logger;
            _mapper = mapper;
            _context = context;
            _rethinkServices = rethinkServices;
            _timezonesEntity = timezonesEntity;
            _cacheService = cacheService;
            _stateRepository = stateRepository;
        }

        /// <summary>
        /// Retrieves a list of claim filing indicators, each represented by a model containing an identifier and a
        /// descriptive indicator.
        /// </summary>
        /// <remarks>This method fetches data asynchronously from the repository. If an error occurs
        /// during the data retrieval, it logs the error and returns an empty list.</remarks>
        /// <returns>A list of ClaimFilingIndicatorModel objects representing the claim filing indicators. The list will be empty
        /// if no indicators are found.</returns>
        public async Task<List<ClaimFilingIndicatorModel>> GetClaimFilingIndicators()
        {
            var ClaimFilingIndicatorModel = new List<ClaimFilingIndicatorModel>();
            try
            {
                // Fetch claim filing indicators from the repository
                var entities = await _claimFilingIndicatorRepo.Query().ToListAsync();
                if (entities.Count > 0)
                {
                    return entities.Select(e => new ClaimFilingIndicatorModel
                    {
                        Id = e.Id,
                        Indicator = $"{e.Code} - {e.Description}"
                    }).ToList();
                }
                return ClaimFilingIndicatorModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching claim filing indicators.");
                return ClaimFilingIndicatorModel;
            }
        }

        /// <summary>
        /// Sets the billing funder settings for the specified funder, adding a new entry or updating an existing one as
        /// required.
        /// </summary>
        /// <remarks>If a funder setting already exists for the provided funder ID, the settings are
        /// updated; otherwise, a new entry is created. Errors encountered during the operation are logged.</remarks>
        /// <param name="model">The request model containing the funder ID and associated billing funder settings to be applied.</param>
        /// <returns>A task that represents the asynchronous operation of setting the billing funder settings.</returns>
        public async Task SetBillingFunderSettings(BillingFunderSettingRequestModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Set or update funder settings
                if (model.FunderId != null)
                {
                    await setFunderSettings(model);
                }

                // Update Billing Features
                if (model.BillingFeatures?.Any() == true)
                {
                    await setBillingFeatures(model);
                }

                // SAVE ONCE
                await _funderSettingRepo.SaveChangesAsync();

                // COMMIT ONCE
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred while saving billing funder settings.");
                throw; // important
            }
        }

        /// <summary>
        /// Retrieves a list of billing funder settings according to the specified sorting and pagination criteria.
        /// </summary>
        /// <remarks>If no sorting models are provided in the request, the method defaults to sorting by
        /// 'Id' in descending order.</remarks>
        /// <param name="model">The request model containing sorting and pagination information for fetching billing funder settings. Cannot
        /// be null.</param>
        /// <returns>A list of BillingFunderSettingResponseModel objects representing the billing funder settings. The list may
        /// be empty if no settings are found.</returns>
        public async Task<BillingFunderSettingResponseModel> GetBillingFunderSettings(BillingFunderListRequestModel model)
        {
            if (model.SortingModels == null || model.SortingModels.Count == 0)
            {
                model.SortingModels =
                [
                    new()
            {
                Dir = "acs",
                Field = "FunderName"
            }
                ];
            }

            try
            {
                // Get data from service
                var funders = await _rethinkServices.GetAllFundersForAccount(model.AccountInfoId);

                var query = funders.AsQueryable();

                // Apply filtering
                if (!string.IsNullOrWhiteSpace(model.FilterModels?.FirstOrDefault()?.Value))
                {
                    var search = model.FilterModels.FirstOrDefault().Value.Trim();

                    query = query.Where(f =>
                        f.funderName != null &&
                        f.funderName.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                var total = query.Count();

                var result = query
                    .ApplySorting(model.SortingModels)
                    .Skip(model.Skip)
                    .Take(model.Take)
                    .Select(x => new BillingFunderSettings
                    {
                        Id = x.id,
                        FunderId = x.id,
                        FunderName = x.funderName,
                        ClearingHousePayerName = x.clearingHousePayerName,
                        ClearingHousePayerId = x.chPayerId,
                        InsuranceType = GetInsuranceTypeName(x.funderTypeId)
                    })
                    .ToList();

                var timeZonesDictionary = _timezonesEntity.Query().Where(x => x.DateDeleted == null)
                                          .ToDictionary(x => x.Id, x => x.Name);

                var accountInfo = await _rethinkServices.GetAccountReturningEntityAsync(model.AccountInfoId, false);
                var showEligblity = accountInfo.subscriptionFeatures != null
                             && accountInfo.subscriptionFeatures.ContainsKey("showEligibility")
                             && (bool)accountInfo.subscriptionFeatures["showEligibility"];

                return new BillingFunderSettingResponseModel
                {
                    Data = result,
                    Total = total,
                    TimeZone = timeZonesDictionary,
                    ClaimFilingIndicator = GetClaimFilingIndicators().Result,
                    ShowEligblity = showEligblity
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching billing funder settings.");

                return new BillingFunderSettingResponseModel
                {
                    Data = [],
                    Total = 0,
                    TimeZone = [],
                    ClaimFilingIndicator = []
                };
            }
        }


        public async Task<BillingFunderIdRequestModel> GetBillingFunderIdsSettingAsync(int funderId, int accountInfoId)
        {
            try
            {
                var entity = await _funderSettingRepo.Query()
                    .Where(f => f.FunderId == funderId &&
                                f.AccountInfoId == accountInfoId &&
                                f.DateDeleted == null)
                    .FirstOrDefaultAsync();

                if (entity == null)
                    return null;

                var monthlyFrequency = entity.MonthlyFrequency;

                var scheduleTimeZone = entity.ScheduleTimeZone;
                
                return new BillingFunderIdRequestModel
                {
                    Id = entity.Id,
                    AccountInfoId = entity.AccountInfoId,
                    FunderId = entity.FunderId,
                    FunderName = entity.FunderName,
                    ScheduleType = entity.ScheduleType,
                    ScheduleTime = entity.ScheduleTime?.ToString(),
                    ScheduleTimeZone = scheduleTimeZone,
                    WeeklyDays = entity.WeeklyDays,
                    MonthlyFrequency = monthlyFrequency,
                    CombineChargesForSameClient = entity.CombineChargesForSameClient,
                    ClaimFilingIndicatorId = entity.ClaimFilingIndicatorId,
                    IncludeTaxonomyCode = entity.IncludeTaxonomyCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching Billing Funder Settings for FunderId: {FunderId}, AccountInfoId: {AccountInfoId}",
                    funderId, accountInfoId);
                return null;
            }
        }


        public async Task<BillingFunderSettingAPIResponse> DeleteFunderSetting(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid FunderSetting Id received: {Id}", id);
                throw new ArgumentException("Invalid Id.");
            }

            var entity = await _funderSettingRepo.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("Funder Setting not found. Id: {Id}", id);
                throw new KeyNotFoundException("Funder setting not found.");
            }

            // Idempotent behavior
            if (entity.DateDeleted.HasValue)
            {
                _logger.LogInformation("FunderSetting already deleted. Id: {Id}", id);

                return new BillingFunderSettingAPIResponse
                {
                    Success = true,
                    Message = "Funder setting already deleted."
                };
            }

            entity.DateDeleted = DateTime.UtcNow;

            await _funderSettingRepo.SaveChangesAsync();

            _logger.LogInformation("Funder Setting soft deleted successfully. Id: {Id}", id);

            return new BillingFunderSettingAPIResponse
            {
                Success = true,
                Message = "Funder setting deleted successfully."
            };
        }

        public async Task<List<FeatureStatusDto>> GetFeaturesForAccountAsync(int accountId)
        {
            if (accountId <= 0)
                throw new ArgumentException("accountId must be greater than zero.", nameof(accountId));

            _logger.LogInformation("Fetching features for accountId={AccountId}", accountId);

            var query =
                from feature in _featureRepository.Query().AsNoTracking()
                join setting in _accountFeatureSettingRepository.Query().AsNoTracking()
                        .Where(x => x.AccountId == accountId)
                    on feature.Id equals setting.FeatureId into fs
                from setting in fs.DefaultIfEmpty()
                orderby feature.Id
                select new FeatureStatusDto
                {
                    FeatureId = feature.Id,
                    FeatureName = feature.FeatureName,
                    IsEnabled = setting != null && setting.Status
                };

            return await query.ToListAsync();
        }

        /// <summary>
        /// Converts the funder type ID to a human-readable insurance type name.
        /// </summary>
        /// <param name="funderTypeId">The funder type identifier.</param>
        /// <returns>A string representing the insurance type name.</returns>
        private static string GetInsuranceTypeName(int? funderTypeId)
        {
            if (!funderTypeId.HasValue)
                return "Unknown";

            return (FunderType)funderTypeId.Value switch
            {
                FunderType.Insurance => "Insurance",
                FunderType.PrivatePay => "Self",
                FunderType.Other => "Other",
                FunderType.SelfPay => "Self",
                _ => "Unknown"
            };
        }

        private async Task setFunderSettings(BillingFunderSettingRequestModel model)
        {
            var entity = _mapper.Map<FunderSettingsEntity>(model);

            var existingEntity = await _funderSettingRepo.Query()
                .FirstOrDefaultAsync(e =>
                    e.FunderId == model.FunderId &&
                    e.AccountInfoId == model.AccountInfoId &&
                    e.DateDeleted == null);

            if (existingEntity == null)
            {
                await _funderSettingRepo.AddAsync(entity);
            }
            else
            {
                existingEntity.ClaimFilingIndicatorId = entity.ClaimFilingIndicatorId;
                existingEntity.IncludeTaxonomyCode = entity.IncludeTaxonomyCode;
                existingEntity.DateLastModified = DateTime.UtcNow;

                await _funderSettingRepo.UpdateAsync(existingEntity);
            }
        }

        /// <summary>
        /// Retrieves billing & statement settings for the provided account.
        /// Returns default values when no settings exist for the account.
        /// </summary>
        public async Task<BillingSettingInformationModel> GetBillingSettingInformationAsync(int accountId)
        {
            if (accountId <= 0)
                throw new ArgumentException("accountId must be greater than zero.", nameof(accountId));

            try
            {
                    // Project only required columns into the DTO and fetch the latest record
                    var billingInformation = await _billingSettingInformationRepository.Query()
                        .AsNoTracking()
                        .Where(x => x.AccountId == accountId && x.DateDeleted == null)
                        .OrderByDescending(x => x.DateLastModified ?? x.DateCreated)
                        .Select(x => new BillingSettingInformationModel
                        {
                            PayToAddressOverrideOption = x.PayToAddressOverrideOption,
                            CompanyName = x.CompanyName,
                            AddressLine1 = x.AddressLine1,
                            AddressLine2 = x.AddressLine2,
                            City = x.City,
                            State = x.State,
                            Zip = x.Zip,
                            ZipExtension = x.ZipExtension,
                            DunningMessage = x.DunningMessage,
                            GlobalMessage = x.GlobalMessage
                        })
                        .FirstOrDefaultAsync();

                    if (billingInformation != null)
                        return billingInformation;

                    return await GetDefaultBillingFromMainLocationAsync(accountId);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching billing setting information for accountId={AccountId}", accountId);
                // Return safe default on error
                return new BillingSettingInformationModel
                {
                    PayToAddressOverrideOption = 0,
                    CompanyName = string.Empty,
                    AddressLine1 = string.Empty,
                    AddressLine2 = string.Empty,
                    City = string.Empty,
                    State = string.Empty,
                    Zip = string.Empty,
                    ZipExtension = string.Empty,
                    DunningMessage = string.Empty,
                    GlobalMessage = string.Empty
                };
            }
        }

        /// <summary>
        /// Retrieves all active states from the database and maps them to <see cref="StateDto"/> objects.
        /// </summary>
        /// <remarks>
        /// Only states that are not soft-deleted (where <c>DateDeleted</c> is null) are returned.
        /// The results are mapped from <see cref="StateEntity"/> to <see cref="StateDto"/> using AutoMapper.
        /// The results are cached for 24 hours to improve performance.
        /// </remarks>
        /// <returns>
        /// A <see cref="List{StateDto}"/> containing all active states. Returns an empty list if no active states exist.
        /// </returns>
        /// <exception cref="Exception">Throws if an unexpected error occurs while retrieving states.</exception>
        private async Task<List<StateDto>> GetStatesAsync()
        {
            try
            {
                // Fetching data from cache, or fetch it from DB if not available
                var data = await _cacheService.GetOrSetCacheAsync(
                    statesCacheKey,
                    async () => await _stateRepository.Query()
                        .AsNoTracking()
                        .Where(x => x.DateDeleted == null)
                        .ToListAsync(),
                    statesCacheDuration
                );

                if (data.Count == 0)
                    return new List<StateDto>();

                var dtos = _mapper.Map<List<StateDto>>(data);
                return dtos;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BillingSettingInformationModel> GetDefaultBillingFromMainLocationAsync(int accountId)
        {
            var mainLocation = await _rethinkServices.GetMainLocation(accountId);
            var states = await GetStatesAsync();

            var stateName = states?
                .FirstOrDefault(s => s.StateId == mainLocation?.address?.stateId)
                ?.StateCode ?? "";

            return new BillingSettingInformationModel
            {
                PayToAddressOverrideOption = 1,
                CompanyName = mainLocation?.agencyName,
                AddressLine1 = mainLocation?.address?.street1,
                AddressLine2 = mainLocation?.address?.street2,
                City = mainLocation?.address?.city,
                State = stateName,
                Zip = mainLocation?.address?.zip,
                ZipExtension = "",
                DunningMessage = "",
                GlobalMessage = ""
            };
        }

        public async Task<ActionResponse> SaveBillingSettingInformationAsync(SaveBillingSettingRequest request, int memberId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.AccountId <= 0)
                throw new ArgumentException("Invalid AccountId");

            if (string.IsNullOrWhiteSpace(request.CompanyName))
                throw new ArgumentException("Company Name is required");

            try
            {
                BillingSettingInformationEntity entity;
                var existingEntity = await _billingSettingInformationRepository.Query()
                    .FirstOrDefaultAsync(x => x.AccountId == request.AccountId && x.DateDeleted == null);

                if (existingEntity == null)
                {

                    entity = new BillingSettingInformationEntity
                    {
                        AccountId = request.AccountId,
                        PayToAddressOverrideOption = request.PayToAddressOverrideOption,
                        CompanyName = request.CompanyName,
                        AddressLine1 = request.AddressLine1,
                        AddressLine2 = request.AddressLine2,
                        City = request.City,
                        State = request.State,
                        Zip = request.Zip,
                        ZipExtension = request.ZipExtension,
                        DunningMessage = request.DunningMessage,
                        GlobalMessage = request.GlobalMessage,
                        CreatedBy = memberId,
                        DateCreated = EstDateTime

                    };

                    await _billingSettingInformationRepository.AddAsync(entity);
                }
                else
                {

                    existingEntity.PayToAddressOverrideOption = request.PayToAddressOverrideOption;
                    existingEntity.CompanyName = request.CompanyName;
                    existingEntity.AddressLine1 = request.AddressLine1;
                    existingEntity.AddressLine2 = request.AddressLine2;
                    existingEntity.City = request.City;
                    existingEntity.State = request.State;
                    existingEntity.Zip = request.Zip;
                    existingEntity.ZipExtension = request.ZipExtension;
                    existingEntity.DunningMessage = request.DunningMessage;
                    existingEntity.GlobalMessage = request.GlobalMessage;
                    existingEntity.ModifiedBy = memberId;
                    existingEntity.DateLastModified = (DateTime?)EstDateTime;

                    _billingSettingInformationRepository.Update(existingEntity);
                    entity = existingEntity;
                }

                await _billingSettingInformationRepository.SaveChangesAsync();

                return ActionResponse.SuccessResult(entity);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error saving billing settings for AccountId={AccountId}",
                    request.AccountId);

                return ActionResponse.FailResult(ex.Message);
            }
        }

        private async Task setBillingFeatures(BillingFunderSettingRequestModel model)
        {
            var featurIds = model.BillingFeatures.Select(x => x.FeatureId).Distinct().ToList();

            var existingFeature = await _accountFeatureSettingRepository.Query().AsNoTracking()
                .Where(x => x.AccountId == model.AccountInfoId && featurIds.Contains(x.FeatureId)).ToListAsync();

            if (existingFeature.Count == 0)
            {
                var billingFeature = new AccountFeatureSettingEntity
                {
                    AccountId = model.AccountInfoId,
                    FeatureId = model.BillingFeatures.First().FeatureId,
                    Status = model.BillingFeatures.First().IsEnabled,
                    DateCreated = DateTime.Now,
                    CreatedBy = 0
                };

                await _accountFeatureSettingRepository.AddAsync(billingFeature);
            }
            else
            {
                var featureToUpdate = new List<AccountFeatureSettingEntity>();
                foreach (var feature in model.BillingFeatures)
                {
                    var billingFeature = existingFeature.FirstOrDefault(x => x.FeatureId == feature.FeatureId);
                    billingFeature.Id = existingFeature.First(x => x.FeatureId == feature.FeatureId).FeatureId;
                    billingFeature.Status = feature.IsEnabled;
                    billingFeature.ModifiedBy = 0;
                    billingFeature.DateLastModified = DateTime.Now;

                    featureToUpdate.Add(billingFeature);
                }

                _accountFeatureSettingRepository.UpdateRange(featureToUpdate);
            }
        }

        private static int ParseInt(string? value, string fieldName)
        {
            if (!int.TryParse(value, out var result))
                throw new ArgumentException($"Invalid {fieldName}");

            return result;
        }


    }
}
