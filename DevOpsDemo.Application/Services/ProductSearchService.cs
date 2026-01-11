using DevOpsDemo.Infrastructure.Entities;
using Nest;

namespace DevOpsDemo.Application.Search;

public sealed class ProductSearchService : IProductSearchService
{
    private const string IndexAlias = "products_current";
    private const int MaxPageSize = 100;

    private readonly IElasticClient _client;

    public ProductSearchService(IElasticClient client)
    {
        _client = client;
    }

    public async Task<ProductSearchResponse> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Min(request.PageSize, MaxPageSize);
        var from = (page - 1) * pageSize;

        var response = await _client.SearchAsync<ProductEntity>(s => s
            .Index(IndexAlias)
            .From(from)
            .Size(pageSize)
            .Query(q => BuildQuery(q, request))
            .Sort(so => BuildSort(so, request))
            .Aggregations(aggs => aggs
                // CATEGORY FACET
                .Terms("category_facet", t => t
                    .Field(p => p.Category)
                    .Size(20)
                )
                // PRICE FACET (RANGES)
                .Range("price_facet", r => r
                    .Field(p => p.Price)
                    .Ranges(
                        rr => rr.To(100),
                        rr => rr.From(100).To(500),
                        rr => rr.From(500).To(1000),
                        rr => rr.From(1000)
                    )
                )
            )
            .Highlight(h => h
                .PreTags("<em>")
                .PostTags("</em>")
                .Fields(
                    hf => hf.Field(p => p.Name),
                    hf => hf.Field(p => p.Description)
                )
            ),
            cancellationToken);

        if (!response.IsValid)
            throw new Exception(response.DebugInformation);

        var categoryAgg = response.Aggregations.Terms("category_facet");
        var priceAgg = response.Aggregations.Range("price_facet");

        return new ProductSearchResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = response.Total,
            Items = response.Hits.Select(hit => new ProductSearchItem
            {
                Id = hit.Source.Id,
                Name = hit.Source.Name,
                Category = hit.Source.Category,
                Price = hit.Source.Price,
                Highlight = hit.Highlight?.Values.SelectMany(v => v).FirstOrDefault()
            }).ToList(),
            CategoryFacets = categoryAgg?.Buckets
                .Select(b => new FacetBucket
                {
                    Key = b.Key,
                    Count = b.DocCount ?? 0
                }).ToList() ?? [],
            PriceFacets = priceAgg?.Buckets
                .Select(b => new FacetBucket
                {
                    Key = $"{b.From ?? 0}-{b.To ?? double.MaxValue}",
                    Count = b.DocCount
                }).ToList() ?? []
        };
    }

    private static QueryContainer BuildQuery(QueryContainerDescriptor<ProductEntity> q, ProductSearchRequest request)
    {
        return q.Bool(b =>
        {
            // FULL-TEXT QUERY
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                b.Must(mu => mu.MultiMatch(mm => mm
                    .Query(request.Query)
                    .Fields(f => f
                        .Field(p => p.Name, 3.0)
                        .Field("name.autocomplete", 2.0)
                        .Field(p => p.Description)
                    )
                    .Type(TextQueryType.BestFields)
                ));
            }

            // FILTERS (non-scoring)
            b.Filter(f =>
            {
                QueryContainer qc = null!;

                if (!string.IsNullOrWhiteSpace(request.Category))
                {
                    qc &= f.Term(t => t
                        .Field(p => p.Category)
                        .Value(request.Category));
                }

                if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
                {
                    qc &= f.Range(r =>
                    {
                        r.Field(p => p.Price);
                        if (request.MinPrice.HasValue)
                            r.GreaterThanOrEquals((double)request.MinPrice.Value);
                        if (request.MaxPrice.HasValue)
                            r.LessThanOrEquals((double)request.MaxPrice.Value);
                        return r;
                    });
                }

                return qc;
            });

            return b;
        });
    }

    private static IPromise<IList<ISort>> BuildSort(SortDescriptor<ProductEntity> sort, ProductSearchRequest request)
    {
        return request.SortBy switch
        {
            "price_asc" => sort.Ascending(p => p.Price),
            "price_desc" => sort.Descending(p => p.Price),
            "newest" => sort.Descending(p => p.CreatedAt),
            _ => sort.Descending(SortSpecialField.Score)
        };
    }
}
