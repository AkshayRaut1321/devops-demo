using DevOpsDemo.Infrastructure.Entities;
using DevOpsDemo.Infrastructure.Interfaces;
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
        var productCollection = _mongoDatabase.GetCollection<ProductEntity>("Products");
        var salesCollection = _mongoDatabase.GetCollection<SaleEntity>("Sales");

        var productCount = await productCollection.CountDocumentsAsync(FilterDefinition<ProductEntity>.Empty);
        if (productCount == 0)
        {
            var random = new Random();

            // Categories and sample product name prefixes
            var categories = new Dictionary<string, string[]>
            {
                ["Electronics - Mobile Phones"] = new[] { "iPhone", "Galaxy", "Pixel", "Redmi", "OnePlus" },
                ["Electronics - Laptops"] = new[] { "MacBook", "Dell XPS", "ThinkPad", "Surface", "HP Spectre" },
                ["Electronics Accessories"] = new[] { "Wireless Mouse", "Keyboard", "Headphones", "Charger", "USB Cable" },
                ["Home Appliances"] = new[] { "Air Conditioner", "Refrigerator", "Washing Machine", "Microwave" },
                ["Home Decor"] = new[] { "Wall Painting", "Decorative Lamp", "Vase", "Cushion" },
                ["Kitchen Appliances"] = new[] { "Mixer Grinder", "Cookware Set", "Blender", "Toaster" },
                ["Sports Equipment"] = new[] { "Football", "Basketball", "Cricket Bat", "Tennis Ball" },
                ["Outdoor Sports"] = new[] { "Tennis Racket", "Golf Club", "Camping Tent", "Hiking Backpack" },
                ["Fitness Equipment"] = new[] { "Treadmill", "Dumbbell Set", "Yoga Mat", "Exercise Bike" },
                ["Men's Fashion"] = new[] { "T-Shirt", "Jeans", "Shirt", "Jacket", "Shoes" },
                ["Women's Fashion"] = new[] { "Dress", "Skirt", "Blouse", "Handbag", "Heels" },
                ["Kids Fashion"] = new[] { "Shorts", "T-Shirt", "Dress", "Sneakers" },
                ["Fashion Accessories"] = new[] { "Sunglasses", "Watch", "Belt", "Wallet", "Scarf" },
                ["Books - Fiction"] = new[] { "Novel", "Story", "Tale", "Mystery" },
                ["Books - Non Fiction"] = new[] { "Biography", "Memoir", "History Book", "Self Help" },
                ["Books - Educational"] = new[] { "Math Textbook", "Science Guide", "English Workbook" },
                ["Toys - Educational"] = new[] { "Puzzle", "Block Set", "Learning Kit" },
                ["Toys - Outdoor"] = new[] { "Swing Set", "Slide", "Trampoline" },
                ["Pet Supplies"] = new[] { "Pet Toy", "Pet Bed", "Collar", "Leash" },
                ["Pet Food"] = new[] { "Dog Food Pack", "Cat Food Pack", "Bird Seeds" }
            };

            var products = new List<ProductEntity>();

            foreach (var category in categories.Keys)
            {
                var names = categories[category];
                for (int i = 0; i < 10; i++) // 10 products per category
                {
                    var name = $"{names[random.Next(names.Length)]}";
                    var price = Math.Round(random.NextDouble() * 490 + 10, 2); // 10.00 to 500.00
                    products.Add(new ProductEntity
                    {
                        Name = name,
                        Description = $"Description for {name}",
                        Category = category,
                        Price = (decimal)price,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await productCollection.InsertManyAsync(products);

            // Seed sales data
            var sales = new List<SaleEntity>();
            foreach (var product in products)
            {
                int salesCount = random.Next(3, 10); // Each product has 3-10 sales records
                for (int j = 0; j < salesCount; j++)
                {
                    sales.Add(new SaleEntity
                    {
                        ProductName = product.Name,
                        Category = product.Category,
                        Quantity = random.Next(1, 20),
                        Price = product.Price,
                        SaleDate = DateTime.Now.AddDays(-random.Next(0, 60)) // Sales in past 60 days
                    });
                }
            }

            await salesCollection.InsertManyAsync(sales);
        }
    }

    public async Task SeedElasticAsync(IElasticIndexService elasticIndexService)
    {
        var productCollection = _mongoDatabase.GetCollection<ProductEntity>("Products");
        
        await elasticIndexService.EnsureIndexAsync();

        //read all products from MongoDB (project to ProductEntity) - implement paging if large.
        var products = await productCollection.Find(FilterDefinition<ProductEntity>.Empty).ToListAsync();
        await elasticIndexService.BulkUpsertAsync(products, batchSize: 200);
    }
}
