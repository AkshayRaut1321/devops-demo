
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ProductEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)] // tells Mongo to store as ObjectId but map to string
    public string? Id { get; set; } 

    [BsonElement("Name")]
    public string Name { get; set; } = null!;

    [BsonElement("Description")]
    public string Description { get; set; } = null!;

    [BsonElement("Category")]
    public string Category { get; set; } = null!;

    [BsonElement("Price")]
    public decimal Price { get; set; }

    [BsonElement("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
