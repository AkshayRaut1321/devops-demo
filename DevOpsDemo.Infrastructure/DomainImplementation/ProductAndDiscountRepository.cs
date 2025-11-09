using AutoMapper;
using DevOpsDemo.Infrastructure.Entities;
using MongoDB.Bson;
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

        private void EnsureIndexes()
        {
            _discountCollection.Indexes.CreateOne(new CreateIndexModel<DiscountEntity>(
                Builders<DiscountEntity>.IndexKeys.Ascending(e => e.ProductId)));
        }
    }
}