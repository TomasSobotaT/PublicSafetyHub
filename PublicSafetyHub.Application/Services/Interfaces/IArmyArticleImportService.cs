namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IArmyArticleImportService
{
    Task<IReadOnlyList<Uri>> GetArticleUrlsAsync();

    Task<bool> ImportAsync(Uri originUrl);
}
