using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DevOpsDemo.IndexerWorker.Entities
{
    public class ChangeStreamCheckpoint
    {
        [BsonId]
        public string Id { get; set; } = default!; // e.g., "products" collection name

        [BsonElement("ResumeToken")]
        public BsonDocument ResumeToken { get; set; } = default!;
        
        [BsonElement("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
