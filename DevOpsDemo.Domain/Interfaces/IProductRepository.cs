using DevOpsDemo.Domain.Models;

public interface IProductRepository
{
    Task<List<Product>> GetPaged(int page, int pageSize);
    Task<(List<Product>, long)> GetPagedWithCount(int page, int pageSize);
    Task<Product?> GetById(string id);
    Task Create(Product product);
    Task Update(Product product);
    Task Delete(string id);
    Task<List<Product>> SearchByFilter(string? category = null, decimal? minPrice = null, decimal? maxPrice = null, string? searchText = null);
    Task<Dictionary<string, object>> GetAggregations();
}