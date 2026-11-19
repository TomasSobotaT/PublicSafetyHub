namespace PublicSafetyHub.Integration.Clients;

public class FirefightersArticlesClient(HttpClient httpClient) : IFirefightersArticlesClient
{
    public Task<HttpResponseMessage> GetArticleAsync(string originUrl)
    {
        return httpClient.GetAsync(originUrl);
    }
}
