namespace DevOpsDemo.Application.Search;

public interface IProductSearchService
{
    Task<ProductSearchResponse> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default);
}
