
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DevOpsDemo.Infrastructure.Entities
{
    public class DiscountEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] // tells Mongo to store as ObjectId but map to string
        public string Id { get; set; } = null!;

        [BsonElement("ProductId")]
        public string ProductId { get; set; } = string.Empty;

        [BsonElement("Percent")]
        public decimal Percent { get; set; }
    }
}