using DevOpsDemo.IndexerWorker.Config;
using Microsoft.Extensions.Options;
using Nest;

namespace DevOpsDemo.IndexerWorker.Infrastructure
{
    public class ElasticClientFactory
    {
        private readonly ILogger<ElasticClientFactory> _logger;
        private readonly ElasticSearchSettings _settings;
        private ElasticClient? _client;

        public ElasticClientFactory(
            IOptions<ElasticSearchSettings> options,
            ILogger<ElasticClientFactory> logger)
        {
            _logger = logger;
            _settings = options.Value;
        }

        private ElasticClient CreateClient()
        {
            _logger.LogInformation("Initializing Elasticsearch client for {Url}", _settings.NodeUrl);

            var uri = new Uri(_settings.NodeUrl);

            var settings = new ConnectionSettings(uri)
                .DefaultIndex(_settings.IndexName)
                .ThrowExceptions()  // Better debugging
                .DisableDirectStreaming(); // Helps logging request/response JSON

            // if (!string.IsNullOrWhiteSpace(_settings.Username))
            // {
            //     settings.BasicAuthentication(_settings.Username, _settings.Password);
            // }

            return new ElasticClient(settings);
        }

        public ElasticClient GetClient()
        {
            if (_client != null)
                return _client;

            _client = CreateClient();
            return _client;
        }
    }
}
