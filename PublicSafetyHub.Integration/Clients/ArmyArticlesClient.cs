using PublicSafetyHub.Core.Configurations;

namespace PublicSafetyHub.Integration.Clients;

public class ArmyArticlesClient(HttpClient httpClient, IArmyArticleImportOptions armyArticleImportOptions) : IArmyArticlesClient
{
    public Task<HttpResponseMessage> GetFeedAsync()
    {
        return httpClient.GetAsync(armyArticleImportOptions.FeedUrl);
    }
}
