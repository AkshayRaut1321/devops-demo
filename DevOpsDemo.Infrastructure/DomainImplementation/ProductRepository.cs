
using AutoMapper;
using DevOpsDemo.Domain.Models;
using DevOpsDemo.Infrastructure.Entities;
using MongoDB.Driver;

namespace DevOpsDemo.Infrastructure.DomainImplementation
{
    public class ProductRepository : IProductRepository
    {
        private readonly IMongoCollection<ProductEntity> _collection;
        private readonly IMapper _mapper;

        public ProductRepository(IMongoDatabase database, IMapper mapper)
        {
            _collection = database.GetCollection<ProductEntity>("Products");
            _mapper = mapper;
            EnsureIndexes();
        }

        // --------------------------
        // CRUD
        // --------------------------

        public async Task<List<Product>> GetPaged(int page, int pageSize)
        {
            var entities = await _collection.Find(_ => true)
                                            .Skip((page - 1) * pageSize)
                                            .Limit(pageSize)
                                            .ToListAsync();
            return _mapper.Map<List<Product>>(entities);
        }

        public async Task<Product?> GetById(string id)
        {
            var entity = await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();
            return entity == null ? null : _mapper.Map<Product>(entity);
        }

        public async Task Create(Product product)
        {
            var entity = _mapper.Map<ProductEntity>(product);

            // Ensure Id is null so MongoDB generates a new ObjectId
            entity.Id = null;

            await _collection.InsertOneAsync(entity);

            // Map generated Id back to domain
            if (entity.Id is null) // safety check
                throw new InvalidOperationException("MongoDB failed to generate an Id.");

            // Update Domain Id after insert
            product.Id = entity.Id;
        }

        public async Task Update(Product product)
        {
            var entity = _mapper.Map<ProductEntity>(product);
            var filter = Builders<ProductEntity>.Filter.Eq(e => e.Id, entity.Id);
            await _collection.ReplaceOneAsync(filter, entity);
        }

        public async Task Delete(string id)
        {
            var filter = Builders<ProductEntity>.Filter.Eq(e => e.Id, id);
            await _collection.DeleteOneAsync(filter);
        }

        // --------------------------
        // Search & Filter
        // --------------------------

        public async Task<List<Product>> SearchByFilter(string? category = null, decimal? minPrice = null, decimal? maxPrice = null, string? searchText = null)
        {
            var filterBuilder = Builders<ProductEntity>.Filter;
            var filters = new List<FilterDefinition<ProductEntity>>();

            if (!string.IsNullOrEmpty(category))
                filters.Add(filterBuilder.Eq(e => e.Category, category));

            if (minPrice.HasValue)
                filters.Add(filterBuilder.Gte(e => e.Price, minPrice.Value));

            if (maxPrice.HasValue)
                filters.Add(filterBuilder.Lte(e => e.Price, maxPrice.Value));

            if (!string.IsNullOrEmpty(searchText))
                filters.Add(filterBuilder.Text(searchText));

            var finalFilter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;
            var entities = await _collection.Find(finalFilter).ToListAsync();
            return _mapper.Map<List<Product>>(entities);
        }

        // --------------------------
        // Aggregations
        // --------------------------

        public async Task<Dictionary<string, object>> GetAggregations()
        {
            var result = new Dictionary<string, object>();

            // Count per category
            var countByCategory = await _collection.Aggregate()
                                                   .Group(e => e.Category, g => new { Category = g.Key, Count = g.Count() })
                                                   .ToListAsync();

            // Average price per category
            var avgPriceByCategory = await _collection.Aggregate()
                                                      .Group(e => e.Category, g => new { Category = g.Key, AvgPrice = g.Average(e => e.Price) })
                                                      .ToListAsync();

            result["CountPerCategory"] = countByCategory;
            result["AvgPricePerCategory"] = avgPriceByCategory;

            return result;
        }

        // --------------------------
        // Index Setup
        // --------------------------

        private void EnsureIndexes()
        {
            _collection.Indexes.CreateOne(new CreateIndexModel<ProductEntity>(
                Builders<ProductEntity>.IndexKeys.Text(e => e.Name).Text(e => e.Description)));

            _collection.Indexes.CreateOne(new CreateIndexModel<ProductEntity>(
                Builders<ProductEntity>.IndexKeys.Ascending(e => e.Category)));

            _collection.Indexes.CreateOne(new CreateIndexModel<ProductEntity>(
                Builders<ProductEntity>.IndexKeys.Descending(e => e.CreatedAt)));
        }
    }
}