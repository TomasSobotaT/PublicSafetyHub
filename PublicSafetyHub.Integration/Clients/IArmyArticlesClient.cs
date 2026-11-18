namespace PublicSafetyHub.Integration.Clients;

public interface IArmyArticlesClient
{
    Task<HttpResponseMessage> GetFeedAsync();
}
