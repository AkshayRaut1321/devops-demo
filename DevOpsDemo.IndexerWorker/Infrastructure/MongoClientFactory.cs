using MongoDB.Driver;

namespace DevOpsDemo.IndexerWorker.Infrastructure;

public class MongoClientFactory
{
    public IMongoClient CreateClient(string connectionString)
    {
        return new MongoClient(connectionString);
    }
}
