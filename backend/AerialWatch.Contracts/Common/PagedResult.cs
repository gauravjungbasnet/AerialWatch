namespace AerialWatch.Contracts.Common;

/// <summary>
/// One page of a server-side list: the rows for this page plus the total that match the filters
/// (so the grid can show "Showing 20 of 131" and the page buttons). Page is 1-based.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
