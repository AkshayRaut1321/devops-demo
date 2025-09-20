using DevOpsDemo.Application.DTOs;

namespace DevOpsDemo.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetPagedAsync(int page, int pageSize);
        Task<ProductDto?> GetByIdAsync(string id);
        Task<ProductDto> CreateAsync(ProductDto productDto);
        Task UpdateAsync(ProductDto productDto);
        Task DeleteAsync(string id);
        Task<List<ProductDto>> SearchByFilterAsync(string? category, decimal? minPrice, decimal? maxPrice, string? searchText);
        Task<Dictionary<string, object>> GetAggregationsAsync();
    }
}