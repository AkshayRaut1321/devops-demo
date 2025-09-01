using System.Threading.Tasks;
using Xunit;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevOpsDemo.Tests
{
    public class HelloTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public HelloTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

        [Fact]
        public async Task Hello_ReturnsHelloText()
        {
            var res = await _client.GetStringAsync("/hello");
            Assert.Contains("Hello World", res);
        }
    }
}