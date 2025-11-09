using AutoMapper;
using DevOpsDemo.Domain.Models;
using DevOpsDemo.Infrastructure.DomainImplementation;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace DevOpsDemo.Infrastructure.Tests.Repositories
{
    public class ProductRepositoryTests : MongoTestBase, IDisposable
    {
        private readonly ProductRepository _repository;

        public ProductRepositoryTests() : base("productsdb")
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            // Create expression and add profile
            var expression = new MapperConfigurationExpression();
            expression.AddProfile(new InfrastructureAutoMapperProfile());

            // Provide a null logger factory (or real if you want logging)
            var config = new MapperConfiguration(expression, loggerFactory: loggerFactory);

            // Create mapper
            var mapper = config.CreateMapper();

            // Use mapper
            _repository = new ProductRepository(_database, mapper);
        }

        #region Create
        [Fact]
        public async Task Create_Should_Insert_And_Return_Product_With_Id()
        {
            var product = new Product
            {
                Name = "Test Product",
                Description = "Test Desc",
                Category = "Books",
                Price = 99.99M
            };

            await _repository.Create(product);
            var saved = await _repository.GetById(product.Id);

            saved.Should().NotBeNull();
            saved!.Name.Should().Be("Test Product");
            saved.Id.Should().NotBeNullOrWhiteSpace();
        }
        #endregion

        #region Update
        [Fact]
        public async Task Update_Should_Modify_Product()
        {
            var product = new Product { Name = "Old Name", Category = "Books", Price = 50 };
            await _repository.Create(product);

            product.Name = "New Name";
            product.Price = 75;

            await _repository.Update(product);
            var updated = await _repository.GetById(product.Id);

            updated!.Name.Should().Be("New Name");
            updated.Price.Should().Be(75);
        }
        #endregion

        #region Delete
        [Fact]
        public async Task Delete_Should_Remove_Product()
        {
            var product = new Product { Name = "To Delete", Category = "Books", Price = 10 };
            await _repository.Create(product);

            await _repository.Delete(product.Id);
            var deleted = await _repository.GetById(product.Id);

            deleted.Should().BeNull();
        }
        #endregion

        #region Pagination
        [Fact]
        public async Task GetPaged_Should_Return_Correct_Number_Of_Items()
        {
            var products = Enumerable.Range(1, 15)
                .Select(i => new Product { Name = $"Product{i}", Category = "Electronics", Price = i })
                .ToList();

            foreach (var p in products)
                await _repository.Create(p);

            var resultPage1 = await _repository.GetPaged(1, 5);
            resultPage1.Count.Should().Be(5);

            var resultPage2 = await _repository.GetPaged(2, 5);
            resultPage2.Count.Should().Be(5);
        }

        [Fact]
        public async Task GetPagedWithCount_Should_Return_Correct_Number_Of_Items()
        {
            var products = Enumerable.Range(1, 15)
                .Select(i => new Product { Name = $"Product{i}", Category = "Electronics", Price = i })
                .ToList();

            foreach (var p in products)
                await _repository.Create(p);

            var resultPage1 = await _repository.GetPagedWithCount(1, 5);
            resultPage1.Item1.Count.Should().Be(5);

            var resultPage2 = await _repository.GetPagedWithCount(2, 5);
            resultPage2.Item1.Count.Should().Be(5);
        }
        #endregion

        #region Search/Filter
        [Fact]
        public async Task SearchByFilter_Should_Return_Correct_Products()
        {
            var products = new List<Product>
            {
                new() { Name = "Phone X", Category = "Electronics", Price = 500 },
                new() { Name = "Phone Y", Category = "Electronics", Price = 300 },
                new() { Name = "Book A", Category = "Books", Price = 20 }
            };

            foreach (var p in products) await _repository.Create(p);

            var result = await _repository.SearchByFilter(category: "Electronics", minPrice: 400);
            result.Count.Should().Be(1);
            result[0].Name.Should().Be("Phone X");
        }
        #endregion

        #region Aggregations
        [Fact]
        public async Task GetAggregations_Should_Return_Correct_Count_And_AvgPrice_WithDictionary()
        {
            var products = new List<Product>
            {
                new() { Name = "Phone X", Category = "Electronics", Price = 500 },
                new() { Name = "Phone Y", Category = "Electronics", Price = 300 },
                new() { Name = "Book A", Category = "Books", Price = 20 }
            };

            foreach (var p in products) await _repository.Create(p);

            var agg = await _repository.GetAggregations();

            // Retrieve object lists
            var countList = (IEnumerable<object>)agg["CountPerCategory"];
            var avgList = (IEnumerable<object>)agg["AvgPricePerCategory"];

            // Manual checks for CountPerCategory
            bool booksCountOk = false;
            bool electronicsCountOk = false;
            foreach (var item in countList)
            {
                var type = item.GetType();
                var category = type.GetProperty("Category")!.GetValue(item)!.ToString();
                var count = (int)type.GetProperty("Count")!.GetValue(item)!;

                if (category == "Books" && count == 1) booksCountOk = true;
                if (category == "Electronics" && count == 2) electronicsCountOk = true;
            }

            booksCountOk.Should().BeTrue();
            electronicsCountOk.Should().BeTrue();

            // Manual checks for AvgPricePerCategory
            bool booksAvgOk = false;
            bool electronicsAvgOk = false;
            foreach (var item in avgList)
            {
                var type = item.GetType();
                var category = type.GetProperty("Category")!.GetValue(item)!.ToString();
                var avg = Convert.ToDecimal(type.GetProperty("AvgPrice")!.GetValue(item)!);

                if (category == "Books" && avg == 20) booksAvgOk = true;
                if (category == "Electronics" && avg == 400) electronicsAvgOk = true;
            }

            booksAvgOk.Should().BeTrue();
            electronicsAvgOk.Should().BeTrue();
        }
        #endregion
    }
}
