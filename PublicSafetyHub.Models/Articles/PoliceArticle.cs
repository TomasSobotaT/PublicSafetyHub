namespace PublicSafetyHub.Models.Articles;

public class PoliceArticle
{
    public string Title { get; set; }

    public string Content { get; set; }

    public string OriginUrl { get; set; }

    public DateTime? PublishedAt { get; set; }
}
