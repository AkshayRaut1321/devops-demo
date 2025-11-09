

public interface ISalesRepository
{
    Task<List<SalesReport>> GetRevenueByCategoryAsync(int skip = 0, int limit = 5);
}