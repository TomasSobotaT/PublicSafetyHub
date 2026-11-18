namespace PublicSafetyHub.Integration.Clients;

public interface IPoliceArticlesClient
{
    Task<HttpResponseMessage> GetListAsync(int pageNumber);

    Task<HttpResponseMessage> GetArticleAsync(string originUrl);
}
