namespace DevOpsDemo.IndexerWorker.Config;

public class WorkerSettings
{
    public int BatchSize { get; set; }
    public string CheckpointCollection { get; set; } = string.Empty;
    public string DeadLetterCollection { get; set; } = string.Empty;
    public bool FullReindexOnStartup { get; set; } = true;
    public int FullReindexIntervalMinutes { get; set; } = 1440;
    public bool ChangeStreamEnabled { get; set; } = true;
    public int ChangeStreamRetrySeconds { get; set; } = 10;
}
