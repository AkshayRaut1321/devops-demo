using DevOpsDemo.Infrastructure.Entities;

namespace DevOpsDemo.Infrastructure.Interfaces
{
    public interface IElasticIndexService
    {
        Task EnsureIndexAsync();
        Task IndexDocumentAsync(ProductEntity product);
        Task BulkIndexAsync(IEnumerable<ProductEntity> products, int batchSize = 100);
        Task<long> CountAsync();
    }
}