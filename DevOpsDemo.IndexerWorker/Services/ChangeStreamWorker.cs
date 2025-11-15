using DevOpsDemo.Infrastructure.Entities;
using DevOpsDemo.Infrastructure.Interfaces;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using DevOpsDemo.IndexerWorker.Config;
using DevOpsDemo.IndexerWorker.Infrastructure;
using DevOpsDemo.IndexerWorker.Entities;
using MongoDB.Bson;

namespace DevOpsDemo.IndexerWorker.Services;

public class ChangeStreamWorker : BackgroundService
{
    private readonly IMongoCollection<ProductEntity> _collection;
    private readonly IElasticIndexService _elasticIndexService;
    private readonly ILogger _logger;
    private readonly WorkerSettings _settings;
    private readonly IMongoCollection<ChangeStreamCheckpoint> _checkpointCollection;

    public ChangeStreamWorker(MongoClientFactory mongoFactory, IElasticIndexService elasticIndexService,
        IOptions<WorkerSettings> workerOptions, IOptions<MongoDbSettings> mongoOptions,
        ILogger<ChangeStreamWorker> logger)
    {
        var workerSettings = workerOptions.Value;
        _settings = workerSettings;

        var mongoDbSettings = mongoOptions.Value;
        _collection = mongoFactory.GetDatabase().GetCollection<ProductEntity>(mongoDbSettings.Collection);
        _checkpointCollection = mongoFactory.GetDatabase()
            .GetCollection<ChangeStreamCheckpoint>(workerSettings.CheckpointCollection);

        _elasticIndexService = elasticIndexService ?? throw new ArgumentNullException(nameof(elasticIndexService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChangeStreamWorker started.");

        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<ProductEntity>>()
                       .Match(change => change.OperationType == ChangeStreamOperationType.Insert
                                     || change.OperationType == ChangeStreamOperationType.Update
                                     || change.OperationType == ChangeStreamOperationType.Replace
                                     || change.OperationType == ChangeStreamOperationType.Delete);

        var options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
            BatchSize = _settings.BatchSize
        };

        // Use resume token if available
        var existingCheckpoint = await _checkpointCollection
            .Find(c => c.Id == _collection.CollectionNamespace.CollectionName)
            .FirstOrDefaultAsync(stoppingToken);

        if (existingCheckpoint != null)
        {
            options.ResumeAfter = existingCheckpoint.ResumeToken;
            _logger.LogInformation("Resuming ChangeStream from saved resume token.");
        }

        try
        {
            using var cursor = await _collection.WatchAsync(pipeline, options, stoppingToken)
                                               .ConfigureAwait(false);

            await cursor.ForEachAsync(async change =>
            {
                // Process document
                switch (change.OperationType)
                {
                    case ChangeStreamOperationType.Insert:
                    case ChangeStreamOperationType.Replace:
                    case ChangeStreamOperationType.Update:
                        if (change.FullDocument != null)
                        {
                            try
                            {
                                await _elasticIndexService.BulkUpsertAsync(
                                    new[] { change.FullDocument },
                                    cancellationToken: stoppingToken);

                                _logger.LogInformation("Upserted document Id={Id} to Elasticsearch.", change.FullDocument.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to upsert document Id={Id}.", change.FullDocument.Id);
                            }
                        }
                        break;

                    case ChangeStreamOperationType.Delete:
                        try
                        {
                            var docId = change.DocumentKey["_id"].AsString;
                            await _elasticIndexService.DeleteAsync(docId, stoppingToken);
                            _logger.LogInformation("Deleted document Id={Id} from Elasticsearch.", docId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete document Id={Id} from Elasticsearch.", change.DocumentKey["_id"]);
                        }
                        break;
                }

                // --- Save resume token after processing ---
                if (change.ResumeToken != null)
                {
                    var filter = Builders<ChangeStreamCheckpoint>.Filter.Eq(c => c.Id, _collection.CollectionNamespace.CollectionName);
                    var update = Builders<ChangeStreamCheckpoint>.Update
                        .Set(c => c.ResumeToken, change.ResumeToken)
                        .Set(c => c.UpdatedAt, DateTime.UtcNow);

                    await _checkpointCollection.UpdateOneAsync(
                        filter,
                        update,
                        new UpdateOptions { IsUpsert = true },
                        stoppingToken);
                }

            }, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ChangeStreamWorker cancellation requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChangeStreamWorker encountered an unhandled exception.");
            throw;
        }
    }

}
