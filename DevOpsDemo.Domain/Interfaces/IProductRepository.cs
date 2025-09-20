public interface IProductRepository
{
    Task<Product> GetByIdAsync(string id);
    Task SaveAsync(Product product);
}