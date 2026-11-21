namespace PublicSafetyHub.Core.Configurations;

public class OpenAiOptions : IOpenAiOptions
{
    public string ApiKey { get; set; }

    public string EmbeddingModel { get; set; }
}
