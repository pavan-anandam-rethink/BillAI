using BillingService.Domain.Extensions;
using BillingService.Domain.Interfaces.FunderSetting;
using BillingService.Domain.Interfaces.History;
using BillingService.Domain.Mapper;
using BillingService.Domain.Models.BillingSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Feature;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Enums.Billing.History;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.FunderSetting;

public class FunderSettingService : IFunderSettingService
{
    private readonly IRepository<BillingDbContext, FunderSettingsEntity> _funderSettingRepo;
    private readonly IRepository<BillingDbContext, ClaimFilingIndicatorEntity> _claimFilingIndicatorRepo;
    private readonly IRepository<BillingDbContext, TimezonesEntity> _timezonesRepo;
    private readonly ILogger<FunderSettingService> _logger;
    private readonly IAuditService _auditService;

    public FunderSettingService(
        IRepository<BillingDbContext, FunderSettingsEntity> funderSettingRepo,
        IAuditService auditService,
        IRepository<BillingDbContext, ClaimFilingIndicatorEntity> claimFilingIndicatorRepo,
        IRepository<BillingDbContext, TimezonesEntity> timezonesRepo,
        ILogger<FunderSettingService> logger)
    {
        _auditService = auditService;
        _funderSettingRepo = funderSettingRepo;
        _claimFilingIndicatorRepo = claimFilingIndicatorRepo;
        _timezonesRepo = timezonesRepo;
        _logger = logger;
    }

    public async Task UpdateFunderSettingsAsync(FunderSettingRequest model)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            ValidateRequest(model.Data);

            var existing = await GetExistingFunderSettingAsync(model.AccountInfoId, model.FunderId);
            var newAuditEntity = await BuildAuditEntityAsync(model);

            if (existing is null)
            {
                await CreateFunderSettingAsync(model, newAuditEntity);
                return;
            }

            await UpdateFunderSettingAsync(model, existing, newAuditEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in Saving Funder Settings for FunderId: {FunderId}, FunderSettingId: {FunderSettingId}",
                model.FunderId, model.Id);

            throw; // optional but recommended
        }
    }

    private static void ValidateRequest(FunderSettingsRequest data)
    {
        ValidateScheduleType(data.ScheduleType);
        ValidateScheduleTime(data.ScheduleTime);
    }

    private static void ValidateScheduleTime(string scheduleTime)
    {
        if (string.IsNullOrWhiteSpace(scheduleTime))
            return;

        if (!TimeSpan.TryParse(scheduleTime, out _))
        {
            throw new ArgumentException(
                $"ScheduleTime must be a valid time value. Provided: {scheduleTime}",
                nameof(scheduleTime));
        }
    }

    private static void ValidateScheduleType(int scheduleType)
    {
        if (!Enum.IsDefined(typeof(ClaimCreationFrequency), scheduleType))
        {
            throw new ArgumentException($"Invalid ScheduleType: {scheduleType}");
        }
    }

    public static bool IsNullOrWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
    private async Task<FunderSettingsAuditEntity> BuildAuditEntityAsync(FunderSettingRequest model)
    {
        var claimDesc = await GetClaimFilingIndicatorDescription(model.ClaimFilingIndicatorId);
        var timezone = await GetTimezoneDisplayName(model.Data.ScheduleTimeZone);

        return model.ToFunderSettingAuditEntity(timezone, claimDesc);
    }

    private async Task<FunderSettingsEntity> GetExistingFunderSettingAsync(int AccountInfoId, int FunderId)
    {
        var existing = await _funderSettingRepo.Query()
            .FirstOrDefaultAsync(f => f.AccountInfoId == AccountInfoId
                                && f.FunderId == FunderId
                                && f.DateDeleted == null);

        return existing;
    }

    private async Task CreateFunderSettingAsync(FunderSettingRequest model,FunderSettingsAuditEntity newAuditEntity)
    {
        var newEntity = model.ToFunderSettingEntity();

        await _funderSettingRepo.AddAsync(newEntity);
        await _funderSettingRepo.SaveChangesAsync();

        await TrackAuditAsync(
            ActionType.I,
            model,
            newEntity.GetType().Name,
            null,
            newAuditEntity);
    }

    private async Task UpdateFunderSettingAsync(FunderSettingRequest model, FunderSettingsEntity existing, FunderSettingsAuditEntity newAuditEntity)
    {
        var oldAuditEntity = await BuildAuditEntityAsync(existing);

        ApplyModelToExisting(existing, model);

        await _funderSettingRepo.UpdateAsync(existing);
        await _funderSettingRepo.SaveChangesAsync();

        await TrackAuditAsync(
            ActionType.U,
            model,
            existing.GetType().Name,
            oldAuditEntity,
            newAuditEntity);
    }

    private static void ApplyModelToExisting(FunderSettingsEntity existing, FunderSettingRequest model)
    {
        var data = model.Data;

        existing.AccountInfoId = model.AccountInfoId;
        existing.FunderId = model.FunderId;
        existing.FunderName = model.FunderName;
        existing.ClaimFilingIndicatorId = model.ClaimFilingIndicatorId;
        existing.IncludeTaxonomyCode = model.IncludeTaxonomyCode;
        existing.ScheduleTime = string.IsNullOrWhiteSpace(data.ScheduleTime)
                              ? null 
                              : TimeSpan.Parse(data.ScheduleTime);
        existing.ScheduleTimeZone = data.ScheduleTimeZone;
        existing.ScheduleType = data.ScheduleType;
        existing.WeeklyDays = data.WeeklyDays;
        existing.MonthlyFrequency = data.MonthlyFrequency;
        existing.CombineChargesForSameClient = data.CombineChargesForSameClient ?? false;
        existing.DateLastModified = DateTime.UtcNow;
        existing.ModifiedBy = model.ChangedBy;
    }

    private async Task TrackAuditAsync(
        ActionType actionType,
        FunderSettingRequest model,
        string entityName,
        FunderSettingsAuditEntity? oldAuditEntity,
        FunderSettingsAuditEntity? newAuditEntity)
    {
        await _auditService.TrackAsync(
            actionType,
            model.ChangedBy,
            model.AccountInfoId,
            model.FunderId,
            entityName,
            oldAuditEntity,
            newAuditEntity,
            [.. AuditExcludedFields.FunderSettings]);

    }

    private async Task<FunderSettingsAuditEntity> BuildAuditEntityAsync(FunderSettingsEntity model)
    {
        var claimDesc = await GetClaimFilingIndicatorDescription(model.ClaimFilingIndicatorId);

        var timezone = await GetTimezoneDisplayName(model.ScheduleTimeZone);
        var scheduleType = model.ScheduleType;

        var monthlyFrequency = model.MonthlyFrequency;

        return model.ToFunderSettingAuditEntity(timezone, claimDesc, scheduleType, monthlyFrequency);
    }

    private async Task<string?> GetClaimFilingIndicatorDescription(int id)
    {
        return await _claimFilingIndicatorRepo.Query()
            .Where(x => x.Id == id)
            .Select(x => x.Description)
            .FirstOrDefaultAsync();
    }

    private async Task<string?> GetTimezoneDisplayName(int? id)
    {
        return await _timezonesRepo.Query()
            .Where(x => x.Id == id)
            .Select(x => x.DisplayName)
            .FirstOrDefaultAsync();
    }
}