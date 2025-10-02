using AutoMapper;
using DevOpsDemo.Application.DTOs;
using DevOpsDemo.Domain.Models;
using DevOpsDemo.Interfaces;

namespace DevOpsDemo.Application
{
    public class ProductAndDiscountService : IProductAndDiscountService
    {
        private readonly IProductAndDiscountRepository _repository;
        private readonly IMapper _mapper;

        public ProductAndDiscountService(IProductAndDiscountRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<ProductDiscount>> GetPagedAsync(int page, int pageSize)
        {
            var products = await _repository.GetPaged(page, pageSize);
            return products;
        }
    }
}
