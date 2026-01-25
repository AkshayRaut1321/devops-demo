using DevOpsDemo.Infrastructure.Interfaces;

namespace DevOpsDemo.IndexerWorker.Services;
public class ElasticBootstrapService : IHostedService
{
    private readonly IElasticIndexService _elasticIndexService;
    private readonly ILogger<ElasticBootstrapService> _logger;

    public ElasticBootstrapService(
        IElasticIndexService elasticIndexService,
        ILogger<ElasticBootstrapService> logger)
    {
        _elasticIndexService = elasticIndexService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ensuring Elasticsearch index and alias...");
        await _elasticIndexService.EnsureIndexAsync();
        _logger.LogInformation("Elasticsearch index and alias ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
