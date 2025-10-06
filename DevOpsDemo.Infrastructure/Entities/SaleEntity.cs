using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DevOpsDemo.Infrastructure.Entities
{
    public class SaleEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    }
}
