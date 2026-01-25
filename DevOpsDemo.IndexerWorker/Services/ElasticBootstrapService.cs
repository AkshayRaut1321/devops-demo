using DevOpsDemo.Infrastructure.Interfaces;

namespace DevOpsDemo.IndexerWorker.Services;

public class ElasticBootstrapService : BackgroundService
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ensuring Elasticsearch index and alias...");
        try
        {
            await _elasticIndexService.EnsureIndexAsync();
            _logger.LogInformation("Elasticsearch index and alias ready.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Elasticsearch index");
            throw; // important
        }
    }
}
