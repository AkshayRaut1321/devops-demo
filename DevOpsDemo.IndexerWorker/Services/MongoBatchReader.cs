using MongoDB.Bson;
using MongoDB.Driver;
using DevOpsDemo.IndexerWorker.Config;
using Microsoft.Extensions.Options;
using DevOpsDemo.Infrastructure.Entities;
using DevOpsDemo.IndexerWorker.Infrastructure;
using System.Runtime.CompilerServices; // ProductEntity

namespace DevOpsDemo.IndexerWorker.Services
{
    /// <summary>
    /// Provides forward-only pagination over the products collection using _id as the cursor.
    /// </summary>
    public class MongoBatchReader
    {
        private readonly MongoClientFactory _mongoFactory;
        private readonly MongoDbSettings _settings;
        private readonly ILogger<MongoBatchReader> _logger;

        public MongoBatchReader(
            MongoClientFactory mongoFactory,
            IOptions<MongoDbSettings> mongoOptions,
            ILogger<MongoBatchReader> logger)
        {
            _mongoFactory = mongoFactory;
            _settings = mongoOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// Paginate using the natural _id ordering. Yields batches of ProductEntity.
        /// NOTE: this implementation assumes ProductEntity has an Id that maps to MongoDB _id (ObjectId or string).
        /// If your Id is string, adjust comparison and casting accordingly.
        /// </summary>
        public async IAsyncEnumerable<List<ProductEntity>> ReadBatchesAsync(int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var collection = _mongoFactory.GetCollection<ProductEntity>();
            var sort = Builders<ProductEntity>.Sort.Ascending("_id"); // use field name to avoid CLR id type assumptions

            BsonValue lastId = BsonNull.Value;

            while (!cancellationToken.IsCancellationRequested)
            {
                FilterDefinition<ProductEntity> filter;
                if (lastId == BsonNull.Value)
                {
                    filter = FilterDefinition<ProductEntity>.Empty;
                }
                else
                {
                    // compare on _id field directly
                    filter = Builders<ProductEntity>.Filter.Gt("_id", lastId);
                }

                var cursor = await collection.Find(filter)
                                           .Sort(sort)
                                           .Limit(batchSize)
                                           .ToListAsync(cancellationToken)
                                           .ConfigureAwait(false);

                if (cursor == null || cursor.Count == 0)
                    yield break;

                // set lastId from the last doc's _id (safe even if CLR property differs)
                // fetch _id directly via BsonDocument projection
                var lastDoc = await collection.Find(filter)
                                              .Sort(sort)
                                              .Limit(batchSize)
                                              .Project("{_id:1}")
                                              .Skip(Math.Max(0, cursor.Count - 1))
                                              .FirstOrDefaultAsync(cancellationToken)
                                              .ConfigureAwait(false);

                if (lastDoc != null)
                {
                    // lastDoc is BsonDocument; but because we used typed collection, it may be ProductEntity.
                    // To be safe, read the _id using a raw query:
                    var raw = await _mongoFactory.GetDatabase()
                                                 .GetCollection<BsonDocument>(_settings.Collection)
                                                 .Find(Builders<BsonDocument>.Filter.Empty)
                                                 .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
                                                 .Limit(1)
                                                 .ToListAsync(cancellationToken)
                                                 .ConfigureAwait(false);
                    // But computing lastId via the typed cursor is simpler:
                    var lastTyped = cursor[^1];
                    // Attempt to read _id via BsonDocument mapping:
                    var bson = lastTyped.ToBsonDocument();
                    lastId = bson.TryGetValue("_id", out var v) ? v : BsonNull.Value;
                }

                yield return cursor;
            }
        }
    }
}
