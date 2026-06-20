namespace DevOpsDemo.IndexerWorker;

public class IndexerReadiness
{
    public bool IsReady { get; private set; }

    public void MarkReady() => IsReady = true;
}