namespace PublicSafetyHub.Core.Configurations;

public class ArticleEmbeddingOptions : IArticleEmbeddingOptions
{
    public bool Enabled { get; set; }

    public double IntervalHours { get; set; }

    public int BatchSize { get; set; }
}
