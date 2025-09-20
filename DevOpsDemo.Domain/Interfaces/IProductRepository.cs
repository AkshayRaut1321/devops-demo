using DevOpsDemo.Domain;

public interface IProductRepository
{
    Task<List<Product>> GetPaged(int page, int pageSize);
    Task<Product?> GetById(string id);
    Task Create(Product product);
    Task Update(Product product);
    Task Delete(string id);
    Task<List<Product>> SearchByFilter(string? category, decimal? minPrice, decimal? maxPrice, string? searchText);
    Task<Dictionary<string, object>> GetAggregations();
}