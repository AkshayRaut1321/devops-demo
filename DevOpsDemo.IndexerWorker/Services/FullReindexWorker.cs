using System.Diagnostics;
using DevOpsDemo.IndexerWorker.Config;
using DevOpsDemo.IndexerWorker.Infrastructure;
using DevOpsDemo.Infrastructure.Entities;
using DevOpsDemo.Infrastructure.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DevOpsDemo.IndexerWorker.Services
{
    /// <summary>
    /// BackgroundService that performs a full reindex from Mongo -> Elasticsearch.
    /// It pages over Mongo using _id as cursor, builds batches, and sends idempotent upserts to ES via IElasticIndexService.
    /// </summary>
    public class FullReindexWorker : BackgroundService
    {
        private readonly MongoClientFactory _mongoFactory;
        private readonly ElasticClientFactory _elasticFactory;
        private readonly IElasticIndexService _elasticIndexService;
        private readonly WorkerSettings _workerSettings;
        private readonly MongoDbSettings _mongoDbSettings;
        private readonly ILogger<FullReindexWorker> _logger;

        public FullReindexWorker(
            MongoClientFactory mongoFactory,
            ElasticClientFactory elasticFactory,
            IElasticIndexService elasticIndexService,
            IOptions<WorkerSettings> workerOptions,
            IOptions<MongoDbSettings> mongoOptions,
            ILogger<FullReindexWorker> logger)
        {
            _mongoFactory = mongoFactory;
            _elasticFactory = elasticFactory;
            _elasticIndexService = elasticIndexService;
            _workerSettings = workerOptions.Value;
            _mongoDbSettings = mongoOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FullReindexWorker started. FullReindexOnStartup={FullReindexOnStartup}", _workerSettings.FullReindexOnStartup);

            if (!_workerSettings.FullReindexOnStartup)
            {
                _logger.LogInformation("Full reindex on startup is disabled. Worker will exit.");
                return;
            }

            try
            {
                await RunFullReindexAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("FullReindexWorker cancellation requested.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in FullReindexWorker.");
                throw;
            }
        }

        private async Task RunFullReindexAsync(CancellationToken cancellationToken)
        {
            var db = _mongoFactory.GetDatabase();
            var collection = db.GetCollection<ProductEntity>(_mongoDbSettings.Collection);

            // Count total documents (fast-ish; may be approximate depending on storage engine)
            var totalCount = await collection.EstimatedDocumentCountAsync(null, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Starting full reindex. Estimated total documents = {Total}", totalCount);

            var batchSize = Math.Max(1, _workerSettings.BatchSize);
            ObjectId? lastObjectId = null;
            long processed = 0;
            int batchNumber = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Build filter: _id > lastObjectId (if lastObjectId is set)
                FilterDefinition<ProductEntity> filter;
                if (lastObjectId.HasValue)
                {
                    filter = Builders<ProductEntity>.Filter.Gt("_id", lastObjectId.Value);
                }
                else
                {
                    filter = FilterDefinition<ProductEntity>.Empty;
                }

                var findOptions = new FindOptions<ProductEntity>
                {
                    Sort = Builders<ProductEntity>.Sort.Ascending("_id"),
                    Limit = batchSize
                };

                var sw = Stopwatch.StartNew();
                var cursor = await collection.FindAsync(filter, findOptions, cancellationToken)
                                            .ConfigureAwait(false);
                var batch = await cursor.ToListAsync(cancellationToken)
                                        .ConfigureAwait(false);
                                        
                if (batch == null || batch.Count == 0)
                {
                    _logger.LogInformation("No more documents to index. Processed = {Processed}", processed);
                    break;
                }

                batchNumber++;
                var batchStart = DateTime.UtcNow;
                _logger.LogInformation("Processing batch {BatchNumber}, size={BatchSize}", batchNumber, batch.Count);

                // Update lastObjectId for next page
                var lastDoc = batch.Last();
                // Attempt to read the underlying _id value. This assumes _id maps to ObjectId.
                BsonValue lastBsonId;
                try
                {
                    lastBsonId = lastDoc.ToBsonDocument().GetValue("_id");
                }
                catch
                {
                    // Fallback: try to read Id property directly if present
                    var idProp = lastDoc.GetType().GetProperty("Id");
                    object? idVal = idProp?.GetValue(lastDoc);
                    lastBsonId = idVal is ObjectId oid ? (BsonValue)oid : (idVal != null ? BsonValue.Create(idVal) : BsonNull.Value);
                }

                if (lastBsonId != BsonNull.Value && lastBsonId.IsObjectId)
                {
                    lastObjectId = lastBsonId.AsObjectId;
                }
                else
                {
                    // If it's not ObjectId, we still proceed but future paging may need adjustment for your Id type.
                    lastObjectId = null;
                }

                // Bulk upsert to Elasticsearch via IElasticIndexService (assumed method: BulkUpsertAsync)
                bool success = false;
                Exception? lastException = null;

                await RetryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        // NOTE: We assume IElasticIndexService has a method like:
                        // Task BulkUpsertAsync<T>(IEnumerable<T> documents, CancellationToken ct)
                        // If your interface differs, adapt this call to match its method signature.
                        await _elasticIndexService.BulkUpsertAsync(batch, cancellationToken: cancellationToken).ConfigureAwait(false);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, "Bulk upsert failed for batch {BatchNumber}. Will retry if policy allows.", batchNumber);
                        throw;
                    }
                }).ConfigureAwait(false);

                if (!success)
                {
                    _logger.LogError(lastException, "Bulk upsert permanently failed for batch {BatchNumber}. Aborting reindex.", batchNumber);
                    throw lastException ?? new Exception("Bulk upsert failed and no exception captured.");
                }

                sw.Stop();
                processed += batch.Count;
                _logger.LogInformation("Batch {BatchNumber} processed. Documents indexed: {Count}. Time={ElapsedMs}ms. TotalProcessed={Processed}/{Total}",
                    batchNumber, batch.Count, sw.ElapsedMilliseconds, processed, totalCount);

                // small delay (throttle) to avoid blasting ES if needed — make configurable later
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Full reindex finished successfully. Total processed = {Processed}", processed);
        }

        /// <summary>
        /// Very small exponential backoff retry policy with jitter.
        /// Production: replace with Polly if you prefer.
        /// </summary>
        private static class RetryPolicy
        {
            public static async Task ExecuteAsync(Func<Task> operation, int maxRetries = 5, int baseDelayMs = 200)
            {
                var rnd = new Random();
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        await operation().ConfigureAwait(false);
                        return;
                    }
                    catch when (attempt < maxRetries)
                    {
                        attempt++;
                        var jitter = rnd.Next(0, 100);
                        var delay = baseDelayMs * (int)Math.Pow(2, attempt - 1) + jitter;
                        await Task.Delay(delay).ConfigureAwait(false);
                        continue;
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
        }
    }
}
