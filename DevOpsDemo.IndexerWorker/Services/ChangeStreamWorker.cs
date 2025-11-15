using DevOpsDemo.IndexerWorker.Config;
using DevOpsDemo.IndexerWorker.Infrastructure;
using Microsoft.Extensions.Options;

namespace DevOpsDemo.IndexerWorker.Services;

public class ChangeStreamWorker : BackgroundService
{
    private readonly ILogger<ChangeStreamWorker> _logger;

    public ChangeStreamWorker(ILogger<ChangeStreamWorker> logger, MongoClientFactory mongoFactory, ElasticClientFactory esFactory,
        IOptions<WorkerSettings> workerSettings)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Indexer Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
