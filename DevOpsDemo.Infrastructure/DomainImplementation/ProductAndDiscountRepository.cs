
using AutoMapper;
using DevOpsDemo.Domain.Models;
using DevOpsDemo.Infrastructure.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace DevOpsDemo.Infrastructure.DomainImplementation
{
    public class ProductAndDiscountRepository : IProductAndDiscountRepository
    {
        private readonly IMongoCollection<ProductEntity> _productsCollection;
        private readonly IMongoCollection<DiscountEntity> _discountCollection;
        private readonly IMapper _mapper;

        public ProductAndDiscountRepository(IMongoDatabase database, IMapper mapper)
        {
            _productsCollection = database.GetCollection<ProductEntity>("Products");
            _discountCollection = database.GetCollection<DiscountEntity>("Discounts");
            _mapper = mapper;
            EnsureIndexes();
        }

        // --------------------------
        // CRUD
        // --------------------------

        public async Task<List<ProductDiscount>> GetPaged(int page, int pageSize)
        {
            // Full outer join pipeline
            var pipeline = new BsonDocument[]
            {
                // Left join: Products → Discounts
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Discounts" },
                    { "localField", "_id" },
                    { "foreignField", "ProductId" },
                    { "as", "discounts" }
                }),
                // Flatten discount array
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$discounts" },
                    { "preserveNullAndEmptyArrays", true }
                }),
                // Right join: Discounts → Products using $unionWith
                new BsonDocument("$unionWith", new BsonDocument
                {
                    { "coll", "Discounts" },
                    { "pipeline", new BsonArray
                        {
                            new BsonDocument("$addFields", new BsonDocument { { "_fromProducts", false } }),
                            new BsonDocument("$lookup", new BsonDocument
                            {
                                { "from", "Products" },
                                { "localField", "ProductId" },
                                { "foreignField", "_id" },
                                { "as", "products" }
                            }),
                            new BsonDocument("$unwind", new BsonDocument
                            {
                                { "path", "$products" },
                                { "preserveNullAndEmptyArrays", true }
                            })
                        }
                    }
                }),
                // Add paging here
                new BsonDocument("$skip", (page - 1) * pageSize),
                new BsonDocument("$limit", pageSize)
            };

            var resultBson = await _productsCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            var fullOuterJoin = resultBson.Select(doc =>
            {
                bool fromProducts = doc.GetValue("_fromProducts", true).AsBoolean;
                var product = fromProducts ? doc : doc.GetValue("products", null)?.AsBsonDocument;
                var discount = fromProducts ? doc.GetValue("discounts", null)?.AsBsonDocument : doc;

                return new ProductDiscount
                {
                    ProductId = product?.GetValue("_id", null)?.ToString(),
                    ProductName = product?.GetValue("Name", null)?.AsString,
                    Category = product?.GetValue("Category", null)?.AsString,
                    Price = product?.GetValue("Price", null)?.AsDecimal,
                    DiscountId = discount?.GetValue("_id", null)?.ToString(),
                    Percent = discount?.GetValue("Percent", null)?.ToDecimal()
                };
            }).ToList();

            return fullOuterJoin;
        }

        // public async Task<Product?> GetById(string id)
        // {
        //     var entity = await _productsCollection.Find(e => e.Id == id).FirstOrDefaultAsync();
        //     return entity == null ? null : _mapper.Map<Product>(entity);
        // }

        // public async Task Create(Product product)
        // {
        //     var entity = _mapper.Map<ProductEntity>(product);

        //     // Ensure Id is null so MongoDB generates a new ObjectId
        //     entity.Id = null;

        //     await _productsCollection.InsertOneAsync(entity);

        //     // Map generated Id back to domain
        //     if (entity.Id is null) // safety check
        //         throw new InvalidOperationException("MongoDB failed to generate an Id.");

        //     // Update Domain Id after insert
        //     product.Id = entity.Id;
        // }

        // public async Task Update(Product product)
        // {
        //     var entity = _mapper.Map<ProductEntity>(product);
        //     var filter = Builders<ProductEntity>.Filter.Eq(e => e.Id, entity.Id);
        //     await _productsCollection.ReplaceOneAsync(filter, entity);
        // }

        // public async Task Delete(string id)
        // {
        //     var filter = Builders<ProductEntity>.Filter.Eq(e => e.Id, id);
        //     await _productsCollection.DeleteOneAsync(filter);
        // }

        // // --------------------------
        // // Search & Filter
        // // --------------------------

        // public async Task<List<Product>> SearchByFilter(string? category = null, decimal? minPrice = null, decimal? maxPrice = null, string? searchText = null)
        // {
        //     var filterBuilder = Builders<ProductEntity>.Filter;
        //     var filters = new List<FilterDefinition<ProductEntity>>();

        //     if (!string.IsNullOrEmpty(category))
        //         filters.Add(filterBuilder.Eq(e => e.Category, category));

        //     if (minPrice.HasValue)
        //         filters.Add(filterBuilder.Gte(e => e.Price, minPrice.Value));

        //     if (maxPrice.HasValue)
        //         filters.Add(filterBuilder.Lte(e => e.Price, maxPrice.Value));

        //     if (!string.IsNullOrEmpty(searchText))
        //         filters.Add(filterBuilder.Text(searchText));

        //     var finalFilter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;
        //     var entities = await _productsCollection.Find(finalFilter).ToListAsync();
        //     return _mapper.Map<List<Product>>(entities);
        // }

        // // --------------------------
        // // Aggregations
        // // --------------------------

        // public async Task<Dictionary<string, object>> GetAggregations()
        // {
        //     var result = new Dictionary<string, object>();

        //     // Count per category
        //     var countByCategory = await _productsCollection.Aggregate()
        //                                            .Group(e => e.Category, g => new { Category = g.Key, Count = g.Count() })
        //                                            .ToListAsync();

        //     // Average price per category
        //     var avgPriceByCategory = await _productsCollection.Aggregate()
        //                                               .Group(e => e.Category, g => new { Category = g.Key, AvgPrice = g.Average(e => e.Price) })
        //                                               .ToListAsync();

        //     result["CountPerCategory"] = countByCategory;
        //     result["AvgPricePerCategory"] = avgPriceByCategory;

        //     return result;
        // }

        // --------------------------
        // Index Setup
        // --------------------------

        private void EnsureIndexes()
        {
            _discountCollection.Indexes.CreateOne(new CreateIndexModel<DiscountEntity>(
                Builders<DiscountEntity>.IndexKeys.Ascending(e => e.ProductId)));
        }
    }
}