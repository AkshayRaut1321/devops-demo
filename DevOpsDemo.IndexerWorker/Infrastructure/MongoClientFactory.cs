using DevOpsDemo.IndexerWorker.Config;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DevOpsDemo.IndexerWorker.Infrastructure;

public class MongoClientFactory
{
    private readonly ILogger<MongoClientFactory> _logger;
    private readonly MongoSettings _settings;
    private IMongoClient? _client;

    public MongoClientFactory(IOptions<MongoSettings> mongoOptions, ILogger<MongoClientFactory> logger)
    {
        _logger = logger;
        _settings = mongoOptions.Value;
    }

    public IMongoClient CreateClient()
    {
        _logger.LogInformation("Initializing MongoClient for {ConnectionString}", _settings.ConnectionString);

        var settings = MongoClientSettings.FromConnectionString(_settings.ConnectionString);

        // Required for change streams
        settings.RetryWrites = true;

        return new MongoClient(settings);
    }

    public IMongoClient GetClient()
    {
        if (_client != null)
            return _client;

        _client = CreateClient();
        return _client;
    }

    public IMongoDatabase GetDatabase()
    {
        return GetClient().GetDatabase(_settings.Database);
    }

    public IMongoCollection<T> GetCollection<T>()
    {
        return GetDatabase().GetCollection<T>(_settings.Collection);
    }
}
