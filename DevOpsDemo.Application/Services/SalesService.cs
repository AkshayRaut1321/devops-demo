using AutoMapper;
using DevOpsDemo.Interfaces;

namespace DevOpsDemo.Application
{
    public class SalesService : ISalesService
    {
        private readonly ISalesRepository _repository;

        public SalesService(ISalesRepository repository, IMapper mapper)
        {
            _repository = repository;
        }

        public async Task<List<SalesReport>> GetRevenueByCategoryAsync()
        {
            return await _repository.GetRevenueByCategoryAsync();
        }
    }
}
