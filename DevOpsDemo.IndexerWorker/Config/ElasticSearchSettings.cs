namespace DevOpsDemo.IndexerWorker.Config;

public class ElasticSearchSettings
{
    public string NodeUrl { get; set; } = default!;
    public string IndexName { get; set; } = default!;
}
