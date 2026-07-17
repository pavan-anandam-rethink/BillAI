using System.Collections.Concurrent;
using System.Text.Json;
using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Domain.Entities.Workflows;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Shared.Models;

namespace BillAI.RulesEngine.Infrastructure.Repositories;

/// <summary>Storage-backed workflow repository.</summary>
public sealed class StorageBackedWorkflowRepository(IStorageProvider storageProvider) : IWorkflowRepository
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WorkflowDefinition>> _cache = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private string BuildKey(Guid id) => $"workflows/{id}.json";
    private ConcurrentDictionary<Guid, WorkflowDefinition> TenantCache(string tenantId)
        => _cache.GetOrAdd(tenantId, _ => new());

    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id, string tenantId, CancellationToken ct = default)
    {
        if (TenantCache(tenantId).TryGetValue(id, out var cached)) return cached;
        var json = await storageProvider.ReadAsync(tenantId, BuildKey(id), ct);
        if (json is null) return null;
        var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(json, _jsonOptions);
        if (workflow is not null) TenantCache(tenantId)[id] = workflow;
        return workflow;
    }

    public async Task<WorkflowDefinition?> GetByNameAsync(string name, string tenantId, CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(tenantId, ct);
        return TenantCache(tenantId).Values.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<PagedResult<WorkflowDefinition>> GetAllAsync(
        string tenantId,
        int page,
        int pageSize,
        WorkflowStatus? status = null,
        CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(tenantId, ct);

        var query = TenantCache(tenantId).Values.AsEnumerable();
        if (status.HasValue) query = query.Where(w => w.Status == status.Value);

        var ordered = query.OrderBy(w => w.Name).ToList();
        return PagedResult<WorkflowDefinition>.Create(
            ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            page, pageSize, ordered.Count);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> GetPublishedWorkflowsAsync(string tenantId, CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(tenantId, ct);
        return TenantCache(tenantId).Values.Where(w => w.Status == WorkflowStatus.Published).ToList();
    }

    public async Task AddAsync(WorkflowDefinition workflow, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(workflow, _jsonOptions);
        await storageProvider.WriteAsync(workflow.TenantId, BuildKey(workflow.Id), json, ct);
        TenantCache(workflow.TenantId)[workflow.Id] = workflow;
    }

    public async Task UpdateAsync(WorkflowDefinition workflow, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(workflow, _jsonOptions);
        await storageProvider.WriteAsync(workflow.TenantId, BuildKey(workflow.Id), json, ct);
        TenantCache(workflow.TenantId)[workflow.Id] = workflow;
    }

    public async Task DeleteAsync(Guid id, string tenantId, CancellationToken ct = default)
    {
        await storageProvider.DeleteAsync(tenantId, BuildKey(id), ct);
        TenantCache(tenantId).TryRemove(id, out _);
    }

    private async Task EnsureCacheLoadedAsync(string tenantId, CancellationToken ct)
    {
        if (TenantCache(tenantId).Count > 0) return;
        var keys = await storageProvider.ListKeysAsync(tenantId, "workflows/", ct);
        foreach (var key in keys)
        {
            var json = await storageProvider.ReadAsync(tenantId, key, ct);
            if (json is null) continue;
            var wf = JsonSerializer.Deserialize<WorkflowDefinition>(json, _jsonOptions);
            if (wf is not null) TenantCache(tenantId)[wf.Id] = wf;
        }
    }
}
