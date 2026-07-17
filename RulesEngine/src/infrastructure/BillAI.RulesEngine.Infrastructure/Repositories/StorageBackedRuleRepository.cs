using System.Collections.Concurrent;
using System.Text.Json;
using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Domain.Entities.Rules;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Shared.Models;

namespace BillAI.RulesEngine.Infrastructure.Repositories;

/// <summary>
/// Storage-backed rule repository that persists each rule as a JSON file via <see cref="IStorageProvider"/>.
/// An in-process cache improves read performance.
/// </summary>
public sealed class StorageBackedRuleRepository(IStorageProvider storageProvider) : IRuleRepository
{
    // In-process read cache: tenantId → ruleId → rule
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, RuleDefinition>> _cache = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private string BuildKey(Guid id) => $"rules/{id}.json";

    private ConcurrentDictionary<Guid, RuleDefinition> TenantCache(string tenantId)
        => _cache.GetOrAdd(tenantId, _ => new());

    /// <inheritdoc/>
    public async Task<RuleDefinition?> GetByIdAsync(Guid id, string tenantId, CancellationToken ct = default)
    {
        if (TenantCache(tenantId).TryGetValue(id, out var cached)) return cached;

        var json = await storageProvider.ReadAsync(tenantId, BuildKey(id), ct);
        if (json is null) return null;

        var rule = JsonSerializer.Deserialize<RuleDefinition>(json, _jsonOptions);
        if (rule is not null) TenantCache(tenantId)[id] = rule;
        return rule;
    }

    /// <inheritdoc/>
    public async Task<RuleDefinition?> GetByNameAsync(string name, string tenantId, CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(tenantId, ct);
        return TenantCache(tenantId).Values
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task<PagedResult<RuleDefinition>> GetAllAsync(
        string tenantId,
        int page,
        int pageSize,
        RuleStatus? status = null,
        string? category = null,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(tenantId, ct);

        var query = TenantCache(tenantId).Values.AsEnumerable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(r =>
                r.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (r.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));

        var ordered = query.OrderBy(r => r.Priority).ThenBy(r => r.Name).ToList();
        var total = ordered.Count;
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return PagedResult<RuleDefinition>.Create(items, page, pageSize, total);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RuleDefinition>> GetPublishedRulesAsync(
        string tenantId,
        string? category = null,
        CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(tenantId, ct);

        var query = TenantCache(tenantId).Values
            .Where(r => r.Status == RuleStatus.Published && r.IsEnabled);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        return query.OrderBy(r => r.Priority).ToList();
    }

    /// <inheritdoc/>
    public async Task AddAsync(RuleDefinition rule, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(rule, _jsonOptions);
        await storageProvider.WriteAsync(rule.TenantId, BuildKey(rule.Id), json, ct);
        TenantCache(rule.TenantId)[rule.Id] = rule;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(RuleDefinition rule, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(rule, _jsonOptions);
        await storageProvider.WriteAsync(rule.TenantId, BuildKey(rule.Id), json, ct);
        TenantCache(rule.TenantId)[rule.Id] = rule;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, string tenantId, CancellationToken ct = default)
    {
        await storageProvider.DeleteAsync(tenantId, BuildKey(id), ct);
        TenantCache(tenantId).TryRemove(id, out _);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string name, string tenantId, CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(tenantId, ct);
        return TenantCache(tenantId).Values.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task EnsureCacheLoadedAsync(string tenantId, CancellationToken ct)
    {
        if (TenantCache(tenantId).Count > 0) return;

        var keys = await storageProvider.ListKeysAsync(tenantId, "rules/", ct);
        foreach (var key in keys)
        {
            var json = await storageProvider.ReadAsync(tenantId, key, ct);
            if (json is null) continue;

            var rule = JsonSerializer.Deserialize<RuleDefinition>(json, _jsonOptions);
            if (rule is not null) TenantCache(tenantId)[rule.Id] = rule;
        }
    }
}
