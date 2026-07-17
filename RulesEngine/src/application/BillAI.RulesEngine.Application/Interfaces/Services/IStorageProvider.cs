namespace BillAI.RulesEngine.Application.Interfaces.Services;

/// <summary>
/// Contract for storing and retrieving serialised rule and workflow definitions.
/// Supports local file system and Azure Blob Storage backends.
/// </summary>
public interface IStorageProvider
{
    /// <summary>Reads the content of a file by its storage key.</summary>
    Task<string?> ReadAsync(string tenantId, string key, CancellationToken ct = default);

    /// <summary>Writes content to a file with the given key.</summary>
    Task WriteAsync(string tenantId, string key, string content, CancellationToken ct = default);

    /// <summary>Deletes a file by its key.</summary>
    Task DeleteAsync(string tenantId, string key, CancellationToken ct = default);

    /// <summary>Returns whether a file with the given key exists.</summary>
    Task<bool> ExistsAsync(string tenantId, string key, CancellationToken ct = default);

    /// <summary>Lists all keys matching an optional prefix.</summary>
    Task<IReadOnlyList<string>> ListKeysAsync(string tenantId, string? prefix = null, CancellationToken ct = default);
}
