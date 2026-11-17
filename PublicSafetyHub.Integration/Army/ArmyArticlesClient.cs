using PublicSafetyHub.Core.Configurations;

namespace PublicSafetyHub.Integration.Army;

public class ArmyArticlesClient(HttpClient httpClient, IArmyArticleImportOptions options) : IArmyArticlesClient
{
    public Task<HttpResponseMessage> GetFeedAsync()
    {
        return httpClient.GetAsync(options.FeedUrl);
    }

    public Task<HttpResponseMessage> GetArticleAsync(Uri originUrl)
    {
        return httpClient.GetAsync(originUrl);
    }
}
