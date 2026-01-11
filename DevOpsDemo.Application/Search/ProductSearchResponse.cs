namespace DevOpsDemo.Application.Search;

public sealed class ProductSearchResponse
{
    public IReadOnlyList<ProductSearchItem> Items { get; init; } = [];
    public long Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class ProductSearchItem
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Category { get; init; } = default!;
    public decimal Price { get; init; }
    public string? Highlight { get; init; }
}
