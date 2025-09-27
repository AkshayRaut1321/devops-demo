using AutoMapper;
using DevOpsDemo.Application.DTOs;
using DevOpsDemo.Domain.Models;
using DevOpsDemo.Interfaces;

namespace DevOpsDemo.Application
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<ProductDto>> GetPagedAsync(int page, int pageSize)
        {
            var products = await _repository.GetPaged(page, pageSize);
            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<ProductDto?> GetByIdAsync(string id)
        {
            var product = await _repository.GetById(id);
            return product == null ? null : _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> CreateAsync(ProductDto productDto)
        {
            var product = _mapper.Map<Product>(productDto);
            await _repository.Create(product);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task UpdateAsync(ProductDto productDto)
        {
            var product = _mapper.Map<Product>(productDto);
            await _repository.Update(product);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.Delete(id);
        }

        public async Task<List<ProductDto>> SearchByFilterAsync(string? category, decimal? minPrice, decimal? maxPrice, string? searchText)
        {
            var products = await _repository.SearchByFilter(category, minPrice, maxPrice, searchText);
            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<Dictionary<string, object>> GetAggregationsAsync()
        {
            return await _repository.GetAggregations();
        }
    }
}
