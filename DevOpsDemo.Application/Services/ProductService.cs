using AutoMapper;
using DevOpsDemo.Application.DTOs;
using DevOpsDemo.Domain;
using DevOpsDemo.Infrastructure;

namespace DevOpsDemo.Application
{
    public class ProductService
    {
        private readonly IProductRepository _repo;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<ProductDto> GetProductAsync(string id)
        {
            var product = await _repo.GetByIdAsync(id);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> SaveProductAsync(ProductDto dto)
        {
            var product = _mapper.Map<Product>(dto);
            await _repo.SaveAsync(product);
            return dto;
        }
    }
}
