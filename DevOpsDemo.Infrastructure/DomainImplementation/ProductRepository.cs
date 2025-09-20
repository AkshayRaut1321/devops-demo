
using DevOpsDemo.Domain;
using MongoDB.Driver;

public class ProductRepository : IProductRepository
{
    private readonly IMongoCollection<ProductEntity> _collection;

    public Task<Product> GetByIdAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(Product product)
    {
        throw new NotImplementedException();
    }

    // Map Product ↔ ProductEntity inside repository
}