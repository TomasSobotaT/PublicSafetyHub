namespace PublicSafetyHub.Integration.Clients;

public class FirefightersArticlesClient(HttpClient httpClient) : IFirefightersArticlesClient
{
    public Task<HttpResponseMessage> GetArticleAsync(string originUrl, CancellationToken cancellationToken)
    {
        return httpClient.GetAsync(originUrl, cancellationToken);
    }
}
