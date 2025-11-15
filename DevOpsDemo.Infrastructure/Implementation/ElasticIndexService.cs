using DevOpsDemo.Infrastructure.Entities;
using DevOpsDemo.Infrastructure.Interfaces;
using Nest;

namespace DevOpsDemo.Infrastructure.Implementation
{
    public class ElasticIndexService : IElasticIndexService
    {
        private readonly IElasticClient _client;
        private readonly string _indexName;

        public ElasticIndexService(IElasticClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _indexName = _client.ConnectionSettings.DefaultIndex ?? "products_v1";
        }

        public async Task EnsureIndexAsync()
        {
            var exists = await _client.Indices.ExistsAsync(_indexName);

            if (exists.Exists)
                return;

            var createResp = await _client.Indices.CreateAsync(_indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                    // Example: add analysis for autocomplete later
                    .Analysis(a => a
                        .Analyzers(ad => ad
                            .Custom("edge_ngram_analyzer", ca => ca
                                .Tokenizer("standard")
                                .Filters("lowercase", "edge_ngram_filter")
                            )
                        )
                        .TokenFilters(tf => tf
                            .EdgeNGram("edge_ngram_filter", eg => eg
                                .MinGram(2)
                                .MaxGram(20)
                            )
                        )
                    )
                )
                .Map<ProductEntity>(m => m
                    .AutoMap() // Auto-map properties where possible
                    .Properties(ps => ps
                        .Text(t => t
                            .Name(n => n.Name)
                            .Fields(f => f.Keyword(k => k.Name("keyword").IgnoreAbove(256)))
                        )
                        .Text(t => t
                            .Name(n => n.Description)
                        )
                        .Keyword(k => k.Name(p => p.Category))
                        .Number(nu => nu.Name(p => p.Price).Type(NumberType.Double))
                        .Date(d => d.Name(p => p.CreatedAt))
                        .Keyword(k => k.Name(p => p.Id))
                    )
                )
            );

            if (!createResp.IsValid)
            {
                throw new Exception($"Failed to create index {_indexName}: {createResp.ServerError?.Error?.ToString() ?? createResp.DebugInformation}");
            }
        }

        public async Task IndexDocumentAsync(ProductEntity product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            var resp = await _client.IndexAsync(product, i => i.Index(_indexName).Id(product.Id));
            if (!resp.IsValid)
                throw new Exception($"Failed to index document id={product.Id}: {resp.DebugInformation}");
        }

        public async Task<long> CountAsync()
        {
            var resp = await _client.CountAsync<ProductEntity>(c => c.Index(_indexName));
            return resp.Count;
        }

        public async Task BulkUpsertAsync(IEnumerable<ProductEntity> products, int batchSize = 500, CancellationToken cancellationToken = default)
        {
            if (products == null) throw new ArgumentNullException(nameof(products));

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
            if (!batch.Any()) return;

            var response = await _client.BulkAsync(b => b
                .Index(_indexName)
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

            var response = await _client.DeleteAsync<ProductEntity>(id, d => d.Index(_indexName), cancellationToken);

            if (!response.IsValid && response.Result != Result.NotFound)
            {
                throw new Exception($"Failed to delete document id={id}: {response.DebugInformation}");
            }
        }

    }
}