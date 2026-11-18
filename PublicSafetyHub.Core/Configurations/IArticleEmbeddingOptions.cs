namespace PublicSafetyHub.Core.Configurations;

public interface IArticleEmbeddingOptions
{
    bool Enabled { get; }

    double IntervalHours { get; }

    int BatchSize { get; }
}
