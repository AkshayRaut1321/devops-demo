using Nest;

namespace DevOpsDemo.IndexerWorker.Infrastructure;

public class ElasticClientFactory
{
    public ElasticClient CreateClient(string url)
    {
        var settings = new ConnectionSettings(new Uri(url));
        return new ElasticClient(settings);
    }
}
