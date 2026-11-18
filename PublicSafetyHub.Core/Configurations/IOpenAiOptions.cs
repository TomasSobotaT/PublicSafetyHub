namespace PublicSafetyHub.Core.Configurations;

public interface IOpenAiOptions
{
    string ApiKey { get; }

    string EmbeddingModel { get; }
}
