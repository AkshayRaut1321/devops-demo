using DevOpsDemo.Domain.Models;

public interface IProductAndDiscountRepository
{
    Task<List<ProductDiscount>> GetPaged(int page, int pageSize);
    // Task Create(Discount discount);
    // Task Update(Discount discount);
    // Task Delete(string id);
    // Task<List<Discount>> SearchByFilter(string? category = null, decimal? minPrice = null, decimal? maxPrice = null, string? searchText = null);
    // Task<Dictionary<string, object>> GetAggregations();
}