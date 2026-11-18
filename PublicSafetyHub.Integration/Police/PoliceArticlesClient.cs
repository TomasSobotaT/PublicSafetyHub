using System.Globalization;
using PublicSafetyHub.Core.Configurations;

namespace PublicSafetyHub.Integration.Police;

public class PoliceArticlesClient(HttpClient httpClient, IPoliceArticleImportOptions options) : IPoliceArticlesClient
{
    public Task<HttpResponseMessage> GetListAsync(int pageNumber)
    {
        return httpClient.GetAsync($"{options.ListPath}?page={pageNumber.ToString(CultureInfo.InvariantCulture)}");
    }

    public Task<HttpResponseMessage> GetArticleAsync(string originUrl)
    {
        return httpClient.GetAsync(originUrl);
    }
}
