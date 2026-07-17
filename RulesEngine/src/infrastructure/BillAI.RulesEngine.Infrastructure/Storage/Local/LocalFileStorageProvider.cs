using BillAI.RulesEngine.Application.Interfaces.Services;

namespace BillAI.RulesEngine.Infrastructure.Storage.Local;

/// <summary>
/// File-system based storage provider. Stores files under a configurable root directory,
/// partitioned by tenant ID.
/// </summary>
public sealed class LocalFileStorageProvider(string rootDirectory) : IStorageProvider
{
    private string TenantPath(string tenantId)
    {
        var path = Path.Combine(rootDirectory, tenantId);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <inheritdoc/>
    public async Task<string?> ReadAsync(string tenantId, string key, CancellationToken ct = default)
    {
        var filePath = BuildFilePath(tenantId, key);
        if (!File.Exists(filePath)) return null;
        return await File.ReadAllTextAsync(filePath, ct);
    }

    /// <inheritdoc/>
    public async Task WriteAsync(string tenantId, string key, string content, CancellationToken ct = default)
    {
        var filePath = BuildFilePath(tenantId, key);
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(filePath, content, ct);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string tenantId, string key, CancellationToken ct = default)
    {
        var filePath = BuildFilePath(tenantId, key);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string tenantId, string key, CancellationToken ct = default)
        => Task.FromResult(File.Exists(BuildFilePath(tenantId, key)));

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> ListKeysAsync(string tenantId, string? prefix = null, CancellationToken ct = default)
    {
        var tenantDir = TenantPath(tenantId);
        var files = Directory.EnumerateFiles(tenantDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(tenantDir, f).Replace(Path.DirectorySeparatorChar, '/'));

        if (!string.IsNullOrWhiteSpace(prefix))
            files = files.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        IReadOnlyList<string> result = files.ToList();
        return Task.FromResult(result);
    }

    private string BuildFilePath(string tenantId, string key)
        => Path.Combine(TenantPath(tenantId), key.Replace('/', Path.DirectorySeparatorChar));
}
