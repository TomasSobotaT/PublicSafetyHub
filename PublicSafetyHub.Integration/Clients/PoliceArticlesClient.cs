using System.Globalization;
using PublicSafetyHub.Core.Configurations;

namespace PublicSafetyHub.Integration.Clients;

public class PoliceArticlesClient(HttpClient httpClient, IPoliceArticleImportOptions policeArticleImportOptions) : IPoliceArticlesClient
{
    public Task<HttpResponseMessage> GetListAsync(int pageNumber)
    {
        return httpClient.GetAsync($"{policeArticleImportOptions.ListPath}?page={pageNumber.ToString(CultureInfo.InvariantCulture)}");
    }

    public Task<HttpResponseMessage> GetArticleAsync(string originUrl)
    {
        return httpClient.GetAsync(originUrl);
    }
}
