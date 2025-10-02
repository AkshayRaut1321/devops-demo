using AutoMapper;
using DevOpsDemo.Domain.Models;
using DevOpsDemo.Infrastructure.DomainImplementation;
using DevOpsDemo.Infrastructure.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DevOpsDemo.Infrastructure.Tests.Repositories
{
    public class ProductAndDiscountRepositoryTests : MongoTestBase, IDisposable
    {
        private readonly ProductAndDiscountRepository _productAndDiscountRepository;
        private readonly ProductRepository _productRepository;

        public ProductAndDiscountRepositoryTests() : base("productsdb")
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            // AutoMapper setup
            var expression = new MapperConfigurationExpression();
            expression.AddProfile(new InfrastructureProfile());
            var config = new MapperConfiguration(expression, loggerFactory);
            var mapper = config.CreateMapper();

            _productAndDiscountRepository = new ProductAndDiscountRepository(_database, mapper);
            _productRepository = new ProductRepository(_database, mapper);
        }

        #region Helpers

        private IMongoCollection<T> GetPrivateCollection<T>(string fieldName)
        {
            return _productAndDiscountRepository.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_productAndDiscountRepository)
                .As<IMongoCollection<T>>();
        }

        private async Task<ProductEntity> InsertProductAsync(string name, string category, decimal price)
        {
            var product = new ProductEntity
            {
                Name = name,
                Category = category,
                Price = price
            };
            await GetPrivateCollection<ProductEntity>("_productsCollection").InsertOneAsync(product);
            return product;
        }

        private async Task<DiscountEntity> InsertDiscountAsync(string? productId, decimal percent)
        {
            var discount = new DiscountEntity
            {
                ProductId = productId,
                Percent = percent
            };
            await GetPrivateCollection<DiscountEntity>("_discountCollection").InsertOneAsync(discount);
            return discount;
        }

        #endregion

        #region Full Outer Join Scenarios

        [Fact]
        public async Task FullOuterJoin_Should_Return_Product_With_No_Discount()
        {
            var product = await InsertProductAsync("Mouse", "Electronics", 25);

            var result = await _productAndDiscountRepository.GetPaged(1, 10);

            result.Should().ContainSingle(r => r.ProductName == "Mouse" && r.Percent == null);
        }

        [Fact]
        public async Task FullOuterJoin_Should_Return_Discount_With_No_Product()
        {
            // Insert a discount with a non-existing ProductId
            var discount = await InsertDiscountAsync(ObjectId.GenerateNewId().ToString(), 15);

            var result = await _productAndDiscountRepository.GetPaged(1, 10);

            result.Should().ContainSingle(r => r.ProductId == null && r.Percent == 15);
        }

        [Fact]
        public async Task FullOuterJoin_Should_Return_Product_With_Discount()
        {
            var product = await InsertProductAsync("Keyboard", "Electronics", 50);
            var discount = await InsertDiscountAsync(product.Id, 10);

            var result = await _productAndDiscountRepository.GetPaged(1, 10);

            // Left join row: product exists, discount may be null
            result.Should().Contain(r =>
                r.ProductId == product.Id &&
                r.ProductName == "Keyboard" &&
                r.Price == 50M
            );

            // Right join row: discount exists, product may be null
            result.Should().Contain(r =>
                r.DiscountId == discount.Id &&
                r.Percent == 10
            );
        }


        #endregion

        #region Pagination

        [Fact]
        public async Task GetPaged_Should_Return_Correct_Number_Of_Items()
        {
            // Clear collections
            await GetPrivateCollection<ProductEntity>("_productsCollection").DeleteManyAsync(FilterDefinition<ProductEntity>.Empty);
            await GetPrivateCollection<DiscountEntity>("_discountCollection").DeleteManyAsync(FilterDefinition<DiscountEntity>.Empty);

            // Insert 10 products
            var products = Enumerable.Range(1, 10)
                .Select(i => new ProductEntity { Name = $"Product{i}", Category = "Books", Price = i * 10 })
                .ToList();

            foreach (var p in products)
                await GetPrivateCollection<ProductEntity>("_productsCollection").InsertOneAsync(p);

            // Page 1
            var page1 = await _productAndDiscountRepository.GetPaged(1, 5);
            page1.Count.Should().Be(5);

            // Page 2
            var page2 = await _productAndDiscountRepository.GetPaged(2, 5);
            page2.Count.Should().Be(5);
        }
        #endregion
    }
}
