namespace PublicSafetyHub.Integration.Army;

public interface IArmyArticlesClient
{
    Task<HttpResponseMessage> GetFeedAsync();
}
