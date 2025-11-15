using DevOpsDemo.Infrastructure.Entities;

namespace DevOpsDemo.Infrastructure.Interfaces
{
    public interface IElasticIndexService
    {
        Task EnsureIndexAsync();
        Task IndexDocumentAsync(ProductEntity product);
        Task<long> CountAsync();
        Task BulkUpsertAsync(IEnumerable<ProductEntity> products, int batchSize = 500, CancellationToken cancellationToken = default);
    }
}