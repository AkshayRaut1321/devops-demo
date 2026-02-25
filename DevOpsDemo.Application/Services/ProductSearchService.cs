using DevOpsDemo.Infrastructure.Entities.Config;
using DevOpsDemo.Infrastructure.Entities.Database;
using Microsoft.Extensions.Options;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace DevOpsDemo.Application.Search;

public sealed class ProductSearchService : IProductSearchService
{
    private readonly string IndexAlias = "products_current";
    private const int MaxPageSize = 100;

    private readonly ElasticsearchClient _client;

    public ProductSearchService(ElasticsearchClient client, IOptions<ElasticSearchSettings> elasticOptions)
    {
        _client = client;
        if (elasticOptions != null && elasticOptions.Value != null)
            IndexAlias = string.IsNullOrWhiteSpace(elasticOptions.Value.IndexAlias) ? IndexAlias : elasticOptions.Value.IndexAlias;
    }

    public async Task<ProductSearchResponse> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);
        var from = (page - 1) * pageSize;

        var response = await _client.SearchAsync<ProductEntity>(s => s
            .Indices(IndexAlias)
            .From(from)
            .Size(pageSize)
            .Query(q => BuildQuery(q, request))
            .Sort(BuildSort(request))
            .Aggregations(aggs => aggs
                // CATEGORY FACET
                .Add("category_facet", a => a
                    .Terms(t => t
                        .Field(p => p.Category)
                        .Size(20)
                    )
                )
                // PRICE FACET (RANGES)
                .Add("price_facet", a => a
                    .Range(r => r
                        .Field(p => p.Price)
                        .Ranges(
                            rr => rr.To(100),
                            rr => rr.From(100).To(500),
                            rr => rr.From(500).To(1000),
                            rr => rr.From(1000)
                        )
                    )
                )
            )
            .Highlight(h => h
                .PreTags("<em>")
                .PostTags("</em>")
                .AddField(p => p.Name)
                .AddField(p => p.Description)
            ),
            cancellationToken);

        if (!response.IsValidResponse)
            throw new Exception(response.DebugInformation);

        StringTermsAggregate? categoryAgg = null;
        RangeAggregate? priceAgg = null;
        if (response.Aggregations is not null)
        {
            response.Aggregations.TryGetAggregate("category_facet", out categoryAgg);
            response.Aggregations.TryGetAggregate("price_facet", out priceAgg);
        }

        return new ProductSearchResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = response.Total,
            Items = response.Hits
                .Where(hit => hit.Source is not null)
                .Select(hit => new ProductSearchItem
                {
                    Id = hit.Source!.Id ?? string.Empty,
                    Name = hit.Source.Name,
                    Category = hit.Source.Category,
                    Price = hit.Source.Price,
                    Highlight = hit.Highlight?.Values.SelectMany(v => v).FirstOrDefault()
                }).ToList(),
            CategoryFacets = categoryAgg?.Buckets
                .Select(b => new FacetBucket
                {
                    Key = b.Key.TryGetString(out var key) ? key ?? string.Empty : b.Key.ToString(),
                    Count = b.DocCount
                }).ToList() ?? [],
            PriceFacets = priceAgg?.Buckets
                .Select(b => new FacetBucket
                {
                    Key = $"{b.From ?? 0}-{b.To ?? double.MaxValue}",
                    Count = b.DocCount
                }).ToList() ?? []
        };
    }

    private Query BuildQuery(QueryDescriptor<ProductEntity> q, ProductSearchRequest request)
    {
        var must = new List<Action<QueryDescriptor<ProductEntity>>>();
        var filter = new List<Action<QueryDescriptor<ProductEntity>>>();

        return q.Bool(b =>
        {
            // FULL-TEXT QUERY
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                must.Add(mu => mu.MultiMatch(mm => mm
                    .Query(request.Query)
                    .Fields(new[] { "name^3",
                    "name.autocomplete^2",
                    "description" })
                    .Type(TextQueryType.BestFields)
                ));
            }

            // FILTERS (non-scoring)
            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                filter.Add(f => f.Term(t => t
                    .Field(p => p.Category)
                    .Value(request.Category!)));
            }

            if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
            {
                filter.Add(f => f.Range(r => r.Number(nr =>
                {
                    nr.Field(p => p.Price);
                    if (request.MinPrice.HasValue)
                        nr.Gte((double)request.MinPrice.Value);
                    if (request.MaxPrice.HasValue)
                        nr.Lte((double)request.MaxPrice.Value);
                })));
            }

            if (must.Count > 0)
                b.Must(must.ToArray());
            if (filter.Count > 0)
                b.Filter(filter.ToArray());
        });
    }

    private Action<SortOptionsDescriptor<ProductEntity>> BuildSort(ProductSearchRequest request)
    {
        return request.SortBy switch
        {
            "price_asc" => s => s.Field(p => p.Price, SortOrder.Asc),
            "price_desc" => s => s.Field(p => p.Price, SortOrder.Desc),
            "newest" => s => s.Field(p => p.CreatedAt, SortOrder.Desc),
            _ => s => s.Score(sc => sc.Order(SortOrder.Desc))
        };
    }
}
