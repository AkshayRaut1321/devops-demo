using MongoDB.Bson;
using MongoDB.Driver;

namespace Playground;

public static class BasicCrud
{
    public static async Task RunAsync(IMongoDatabase db)
    {
        var products = db.GetCollection<BsonDocument>("sample");

        Console.WriteLine("=== BASIC CRUD DEMO ===");

        // INSERT
        var doc = new BsonDocument {
            { "Name", "Wireless Mouse" },
            { "Category", "Electronics" },
            { "Price", 29.99m },
            { "CreatedAt", DateTime.UtcNow }
        };
        await products.InsertOneAsync(doc);
        Console.WriteLine($"Inserted document with Id: {doc["_id"]}");

        // FIND
        var found = await products.Find(new BsonDocument { { "Category", "Electronics" } })
                                  .ToListAsync();
        Console.WriteLine($"Found {found.Count} electronics");

        // UPDATE
        var filter = Builders<BsonDocument>.Filter.Eq("Name", "Wireless Mouse");
        var update = Builders<BsonDocument>.Update.Set("Price", 24.99m);
        var updateResult = await products.UpdateOneAsync(filter, update);
        Console.WriteLine($"Updated {updateResult.ModifiedCount} document(s)");

        // DELETE
        var deleteFilter = Builders<BsonDocument>.Filter.Eq("Name", "Wireless Mouse");
        var deleteResult = await products.DeleteOneAsync(deleteFilter);
        Console.WriteLine($"Deleted {deleteResult.DeletedCount} document(s)");
    }
}
