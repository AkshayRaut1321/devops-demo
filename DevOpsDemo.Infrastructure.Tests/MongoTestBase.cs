using Mongo2Go;
using MongoDB.Driver;

public abstract class MongoTestBase : IDisposable
{
    protected readonly MongoDbRunner _runner;
    protected readonly IMongoDatabase _database;

    protected MongoTestBase(string dbName)
    {
        _runner = MongoDbRunner.Start();
        var client = new MongoClient(_runner.ConnectionString);
        _database = client.GetDatabase(dbName);
    }

    public void Dispose() => _runner.Dispose();
}
