namespace PublicSafetyHub.Integration.Clients;

public interface IFirefightersArticlesClient
{
    Task<HttpResponseMessage> GetArticleAsync(string originUrl, CancellationToken cancellationToken);
}
