using System.Collections.Concurrent;
using System.Text.Json;
using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Domain.Entities.Workflows;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Shared.Models;

namespace BillAI.RulesEngine.Infrastructure.Repositories;

/// <summary>Storage-backed workflow instance repository.</summary>
public sealed class StorageBackedWorkflowInstanceRepository(IStorageProvider storageProvider) : IWorkflowInstanceRepository
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WorkflowInstance>> _cache = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private string BuildKey(Guid id) => $"workflow-instances/{id}.json";
    private ConcurrentDictionary<Guid, WorkflowInstance> TenantCache(string tenantId)
        => _cache.GetOrAdd(tenantId, _ => new());

    public async Task<WorkflowInstance?> GetByIdAsync(Guid id, string tenantId, CancellationToken ct = default)
    {
        if (TenantCache(tenantId).TryGetValue(id, out var cached)) return cached;
        var json = await storageProvider.ReadAsync(tenantId, BuildKey(id), ct);
        if (json is null) return null;
        var instance = JsonSerializer.Deserialize<WorkflowInstance>(json, _jsonOptions);
        if (instance is not null) TenantCache(tenantId)[id] = instance;
        return instance;
    }

    public async Task<PagedResult<WorkflowInstance>> GetAllAsync(
        string tenantId,
        int page,
        int pageSize,
        WorkflowInstanceStatus? status = null,
        Guid? workflowId = null,
        CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(tenantId, ct);

        var query = TenantCache(tenantId).Values.AsEnumerable();
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (workflowId.HasValue) query = query.Where(i => i.WorkflowId == workflowId.Value);

        var ordered = query.OrderByDescending(i => i.CreatedAt).ToList();
        return PagedResult<WorkflowInstance>.Create(
            ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            page, pageSize, ordered.Count);
    }

    public async Task AddAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(instance, _jsonOptions);
        await storageProvider.WriteAsync(instance.TenantId, BuildKey(instance.Id), json, ct);
        TenantCache(instance.TenantId)[instance.Id] = instance;
    }

    public async Task UpdateAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(instance, _jsonOptions);
        await storageProvider.WriteAsync(instance.TenantId, BuildKey(instance.Id), json, ct);
        TenantCache(instance.TenantId)[instance.Id] = instance;
    }

    private async Task EnsureCacheLoadedAsync(string tenantId, CancellationToken ct)
    {
        if (TenantCache(tenantId).Count > 0) return;
        var keys = await storageProvider.ListKeysAsync(tenantId, "workflow-instances/", ct);
        foreach (var key in keys)
        {
            var json = await storageProvider.ReadAsync(tenantId, key, ct);
            if (json is null) continue;
            var inst = JsonSerializer.Deserialize<WorkflowInstance>(json, _jsonOptions);
            if (inst is not null) TenantCache(tenantId)[inst.Id] = inst;
        }
    }
}
