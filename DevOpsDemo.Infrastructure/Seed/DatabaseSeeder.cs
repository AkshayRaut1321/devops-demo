using DevOpsDemo.Infrastructure.Entities;
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

            // List of categories
            var categories = new[]
            {
                "Electronics - Mobile Phones",
                "Electronics - Laptops",
                "Electronics Accessories",
                "Home Appliances",
                "Home Decor",
                "Kitchen Appliances",
                "Sports Equipment",
                "Outdoor Sports",
                "Fitness Equipment",
                "Men's Fashion",
                "Women's Fashion",
                "Kids Fashion",
                "Fashion Accessories",
                "Books - Fiction",
                "Books - Non Fiction",
                "Books - Educational",
                "Toys - Educational",
                "Toys - Outdoor",
                "Pet Supplies",
                "Pet Food"
            };

            var products = Enumerable.Range(1, 200).Select(i => new ProductEntity
            {
                Name = $"Product {i}",
                Description = $"Description {i}",
                Category = categories[random.Next(categories.Length)], // Random category
                Price = random.Next(10, 500),
                CreatedAt = DateTime.Now
            });

            await collection.InsertManyAsync(products);
        }
    }
}
