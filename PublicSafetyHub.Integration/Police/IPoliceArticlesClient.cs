namespace PublicSafetyHub.Integration.Police;

public interface IPoliceArticlesClient
{
    Task<HttpResponseMessage> GetListAsync(int pageNumber);

    Task<HttpResponseMessage> GetArticleAsync(string originUrl);
}
