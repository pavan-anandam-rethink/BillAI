namespace ClaimLifecycleMonitoring.Contracts.Common;

/// <summary>
/// Envelope describing a paged result set.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Zero-based page index.</summary>
    public int Page { get; init; }

    /// <summary>Requested page size.</summary>
    public int PageSize { get; init; }

    /// <summary>Total items matching the query.</summary>
    public long TotalCount { get; init; }

    /// <summary>Items on the current page.</summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>Creates a new <see cref="PagedResult{T}"/>.</summary>
    public PagedResult() { }

    /// <summary>Creates a populated <see cref="PagedResult{T}"/>.</summary>
    public PagedResult(int page, int pageSize, long totalCount, IReadOnlyList<T> items)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        Items = items;
    }
}
