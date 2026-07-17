namespace BillAI.RulesEngine.Shared.Models;

/// <summary>
/// Represents a paginated list of items.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Gets the items in the current page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Gets the current page number (1-based).</summary>
    public int Page { get; init; }

    /// <summary>Gets the number of items per page.</summary>
    public int PageSize { get; init; }

    /// <summary>Gets the total number of items across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Gets whether there is a previous page.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Gets whether there is a next page.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Creates a <see cref="PagedResult{T}"/> from a list and pagination parameters.</summary>
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        => new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
}
