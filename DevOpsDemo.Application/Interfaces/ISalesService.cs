

namespace DevOpsDemo.Interfaces
{
    public interface ISalesService
    {
        Task<List<SalesReport>> GetRevenueByCategoryAsync();
    }
}