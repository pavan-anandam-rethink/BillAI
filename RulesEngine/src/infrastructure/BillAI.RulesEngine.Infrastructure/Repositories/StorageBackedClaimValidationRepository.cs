using System.Collections.Concurrent;
using System.Text.Json;
using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Domain.Entities.Claims;
using BillAI.RulesEngine.Shared.Models;

namespace BillAI.RulesEngine.Infrastructure.Repositories;

/// <summary>Storage-backed claim validation record repository.</summary>
public sealed class StorageBackedClaimValidationRepository(IStorageProvider storageProvider) : IClaimValidationRepository
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, ClaimValidationRecord>> _cache = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private string BuildKey(Guid id) => $"claim-validations/{id}.json";
    private ConcurrentDictionary<Guid, ClaimValidationRecord> TenantCache(string tenantId)
        => _cache.GetOrAdd(tenantId, _ => new());

    public async Task<ClaimValidationRecord?> GetByIdAsync(Guid id, string tenantId, CancellationToken ct = default)
    {
        if (TenantCache(tenantId).TryGetValue(id, out var cached)) return cached;
        var json = await storageProvider.ReadAsync(tenantId, BuildKey(id), ct);
        if (json is null) return null;
        var record = JsonSerializer.Deserialize<ClaimValidationRecord>(json, _jsonOptions);
        if (record is not null) TenantCache(tenantId)[id] = record;
        return record;
    }

    public async Task<ClaimValidationRecord?> GetByAuditIdAsync(Guid auditId, string tenantId, CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(tenantId, ct);
        return TenantCache(tenantId).Values.FirstOrDefault(r => r.AuditId == auditId);
    }

    public async Task<PagedResult<ClaimValidationRecord>> GetAllAsync(
        string tenantId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(tenantId, ct);
        var ordered = TenantCache(tenantId).Values.OrderByDescending(r => r.CreatedAt).ToList();
        return PagedResult<ClaimValidationRecord>.Create(
            ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            page, pageSize, ordered.Count);
    }

    public async Task AddAsync(ClaimValidationRecord record, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(record, _jsonOptions);
        await storageProvider.WriteAsync(record.TenantId, BuildKey(record.Id), json, ct);
        TenantCache(record.TenantId)[record.Id] = record;
    }

    private async Task EnsureCacheLoadedAsync(string tenantId, CancellationToken ct)
    {
        if (TenantCache(tenantId).Count > 0) return;
        var keys = await storageProvider.ListKeysAsync(tenantId, "claim-validations/", ct);
        foreach (var key in keys)
        {
            var json = await storageProvider.ReadAsync(tenantId, key, ct);
            if (json is null) continue;
            var rec = JsonSerializer.Deserialize<ClaimValidationRecord>(json, _jsonOptions);
            if (rec is not null) TenantCache(tenantId)[rec.Id] = rec;
        }
    }
}
