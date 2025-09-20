

using MongoDB.Driver;

public class DatabaseSeeder
{
    private readonly IMongoDatabase _mongoDatabase;

    public DatabaseSeeder(IMongoDatabase mongoDatabase)
    {
        _mongoDatabase = mongoDatabase;
    }

    public async Task SeedAsync()
    {
        var collection = _mongoDatabase.GetCollection<ProductEntity>("Products");

        var count = await collection.CountDocumentsAsync(FilterDefinition<ProductEntity>.Empty);

        if (count == 0)
        {
            var random = new Random();
            var products = Enumerable.Range(1, 200).Select(i => new ProductEntity
            {
                Name = $"Product {i}",
                Description = $"Description {i}",
                Category = i % 5 == 0 ? "Electronics" : "Books",
                Price = random.Next(10, 500),
                CreatedAt = DateTime.Now
            });

            await collection.InsertManyAsync(products);
        }
    }
}