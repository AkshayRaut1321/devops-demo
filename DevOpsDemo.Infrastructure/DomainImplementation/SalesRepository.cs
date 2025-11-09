using AutoMapper;
using DevOpsDemo.Infrastructure.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DevOpsDemo.Infrastructure.DomainImplementation
{
    public class SalesRepository : ISalesRepository
    {
        private readonly IMongoCollection<SaleEntity> _sales;
        private readonly IMapper _mapper;

        public SalesRepository(IMongoDatabase database, IMapper mapper)
        {
            _sales = database.GetCollection<SaleEntity>("Sales");
            _mapper = mapper;
        }

        public async Task<List<SalesReport>> GetRevenueByCategoryAsync(int skip = 0, int limit = 5)
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("Price", new BsonDocument("$gt", 10))),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Products" },
                    { "let", new BsonDocument("productName", "$ProductName") },
                    { "pipeline", new BsonArray
                        {
                            new BsonDocument("$match", new BsonDocument("$expr",
                                new BsonDocument("$and", new BsonArray
                                {
                                    new BsonDocument("$eq", new BsonArray{"$Name", "$$productName"}),
                                    new BsonDocument("$gt", new BsonArray{"$Price", 20})
                                })))
                        } },
                    { "as", "productInfo" }
                }),
                new BsonDocument("$match", new BsonDocument("productInfo", new BsonDocument("$ne", new BsonArray()))),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$Category" }, //grouping with Single property
                    // { "_id", new BsonDocument //grouping with multiple property, //grouping with Single property
                    //     {
                    //         { "category", "$category" },
                    //         { "brand", "$brand"}
                    //     }
                    // },
                    { "totalRevenue", new BsonDocument("$sum", new BsonDocument("$multiply", new BsonArray{"$Price", "$Quantity"})) },
                    { "orderCount", new BsonDocument("$sum", 1) }
                }),
                new BsonDocument("$match", new BsonDocument("totalRevenue", new BsonDocument("$gt", 100))),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "category", "$_id" }, //projection from single group by property
                    // { "category", "$_id.category" }, { "brand", "$_id.brand" }, //projection from multiple group by properties
                    { "totalRevenue", 1 },
                    { "orderCount", 1 }
                }),
                new BsonDocument("$sort", new BsonDocument("totalRevenue", -1)),
                new BsonDocument("$skip", skip),
                new BsonDocument("$limit", limit)
            };

            var result = await _sales.Aggregate<BsonDocument>(pipeline).ToListAsync();
            var salesReport = _mapper.Map<List<SalesReport>>(result);
            return salesReport;
        }
    }
}