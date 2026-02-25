using DevOpsDemo.Infrastructure.Entities.Config;
using DevOpsDemo.Infrastructure.Entities.Database;
using DevOpsDemo.Infrastructure.Interfaces;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsDemo.Infrastructure.Implementation
{
    public class ElasticIndexService : IElasticIndexService
    {
        private readonly ElasticsearchClient _client;
        private readonly string _indexName = "products_v1";
        private readonly string _indexAlias = "products_current";
        private readonly ILogger _logger;

        public ElasticIndexService(ElasticsearchClient client, ILogger<ElasticIndexService> logger,
        IOptions<ElasticSearchSettings> options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (options != null && options.Value != null)
            {
                ElasticSearchSettings _settings = options.Value;
                _indexName = string.IsNullOrWhiteSpace(_settings.IndexName) ? _indexName : _settings.IndexName;
                _indexAlias = string.IsNullOrWhiteSpace(_settings.IndexAlias) ? _indexAlias : _settings.IndexAlias;
            }
        }

        public async Task EnsureIndexAsync()
        {
            // 1. Check if alias already exists
            var aliasResponse = await _client.Indices.GetAliasAsync(a => a.Name(_indexAlias));
            if (aliasResponse.IsValidResponse && aliasResponse.Aliases != null && aliasResponse.Aliases.Count > 0)
            {
                if (aliasResponse.Aliases.ContainsKey(_indexName))
                {
                    _logger.LogInformation(
                        "Alias '{Alias}' already correctly points to index '{Index}'. Skipping creation.",
                        _indexAlias, _indexName);

                    return;
                }
                _logger.LogInformation("Alias exists but points to a different index. Will update.");
            }
            // 2. Index exists?
            var indexExists = await _client.Indices.ExistsAsync(_indexName);
            if (!indexExists.Exists)
            {
                // 3. Create physical index
                var createIndexResponse = await _client.Indices.CreateAsync(_indexName, c => c
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(0)
                        .Analysis(a => a
                            .TokenFilters(tf => tf
                                .EdgeNGram("edge_ngram_filter", eg => eg
                                    .MinGram(2)
                                    .MaxGram(20)
                                )
                            )
                            .Analyzers(an => an
                                .Custom("autocomplete_analyzer", ca => ca
                                    .Filter(new[] { "lowercase", "edge_ngram_filter" })
                                    .Tokenizer("standard")
                                )
                            )
                        )
                    )
                    .Mappings(m => m
                        .Properties<ProductEntity>(ps => ps
                            // ID
                            .Keyword(p => p.Id)

                            // Name: full text + keyword + autocomplete  
                            .Text(p => p.Name, t => t
                                .Analyzer("standard")
                                .Fields(f => f
                                    .Keyword("keyword")
                                    .Text("autocomplete", tt => tt
                                        .Analyzer("autocomplete_analyzer")
                                        .SearchAnalyzer("standard")
                                    )
                                )
                            )

                            // Description: full text
                            .Text(p => p.Description, t => t
                                .Analyzer("standard")
                            )

                            // Category: filterable + sortable
                            .Keyword(p => p.Category)

                            // Price: numeric filtering/sorting
                            .IntegerNumber(p => p.Price)

                            // CreatedAt: sorting
                            .Date(p => p.CreatedAt)
                        )
                    )
                );

                if (!createIndexResponse.IsValidResponse)
                    throw new Exception(createIndexResponse.DebugInformation);

                _logger.LogInformation($"Elasticsearch index '{_indexName}' created.");
            }

            _logger.LogInformation($"Creating alias '{_indexAlias}'.");

            // 4. Create alias
            var updateAliasResponse = await _client.Indices.UpdateAliasesAsync(a => a
                .Actions(actions => actions
                    .Add(add => add.Index(_indexName).Alias(_indexAlias))
                )
            );

            if (!updateAliasResponse.IsValidResponse)
                throw new Exception(updateAliasResponse.DebugInformation);

            _logger.LogInformation($"Elasticsearch index alias '{_indexAlias}' created.");
        }

        public async Task IndexDocumentAsync(ProductEntity product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            var resp = await _client.IndexAsync(product, i => i.Index(_indexAlias).Id(product.Id));
            if (!resp.IsValidResponse)
                throw new Exception($"Failed to index document id={product.Id}: {resp.DebugInformation}");
        }

        public async Task<long> CountAsync()
        {
            var resp = await _client.CountAsync<ProductEntity>(c => c.Indices(_indexAlias));
            return resp.Count;
        }

        public async Task BulkUpsertAsync(IEnumerable<ProductEntity> products, int batchSize = 500, CancellationToken cancellationToken = default)
        {
            if (products == null)
                throw new ArgumentNullException(nameof(products));

            var batch = new List<ProductEntity>(batchSize);

            foreach (var product in products)
            {
                batch.Add(product);

                if (batch.Count >= batchSize)
                {
                    await SendBulk(batch, cancellationToken);
                    batch.Clear();
                }
            }

            // send remaining documents
            if (batch.Count > 0)
                await SendBulk(batch, cancellationToken);
        }

        private async Task SendBulk(IEnumerable<ProductEntity> batch, CancellationToken cancellationToken)
        {
            if (!batch.Any())
                return;

            var response = await _client.BulkAsync(b => b
                .Index(_indexAlias)
                .IndexMany(batch, (bi, doc) => bi.Id(doc.Id)), // ensures idempotency
                cancellationToken
            ).ConfigureAwait(false);

            if (response.Errors)
            {
                // log individual failures
                var itemsWithErrors = response.ItemsWithErrors
                                              .Select(i => $"Id: {i.Id}, Reason: {i.Error?.Reason}");
                // replace Console.WriteLine with Serilog in your worker if desired
                Console.WriteLine($"Bulk upsert errors: {string.Join("; ", itemsWithErrors)}");

                throw new Exception("Bulk upsert to Elasticsearch encountered errors.");
            }
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            var response = await _client.DeleteAsync<ProductEntity>(id, d => d.Index(_indexAlias), cancellationToken);

            _logger.LogInformation("ES delete response for Id={Id}. Result={Result}, StatusCode={Status}, Valid={Valid}",
            id, response.Result, response.ApiCallDetails?.HttpStatusCode, response.IsValidResponse);

            if (!response.IsValidResponse)
            {
                response.TryGetOriginalException(out var ex);
                _logger.LogError(ex, "ES delete failed for Id={Id}", id);
                throw new Exception($"Failed to delete document id={id}: {response.DebugInformation}");
            }
        }
    }
}