namespace DevOpsDemo.Infrastructure.Entities.Config;

public class ElasticSearchSettings
{
    public string NodeUrl { get; set; } = default!;
    public string IndexName { get; set; } = default!;
    public string IndexAlias { get; set; } = default!;

    public string? Username { get; set; }
    public string? Password { get; set; }
}
