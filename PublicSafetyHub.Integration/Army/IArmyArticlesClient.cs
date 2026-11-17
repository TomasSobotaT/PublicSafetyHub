namespace PublicSafetyHub.Integration.Army;

public interface IArmyArticlesClient
{
    Task<HttpResponseMessage> GetFeedAsync();

    Task<HttpResponseMessage> GetArticleAsync(Uri originUrl);
}
