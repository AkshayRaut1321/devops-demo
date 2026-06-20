namespace DevOpsDemo.Application.Search;

public sealed class ProductSearchRequest
{
    public string? Query { get; init; }
    public string? Category { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    // relevance | price_asc | price_desc | newest
    public string SortBy { get; init; } = "relevance";
}