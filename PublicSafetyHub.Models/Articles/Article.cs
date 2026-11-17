using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Models.Articles;

public class Article
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string Content { get; set; }

    public string OriginUrl { get; set; }

    public ArticleType Type { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
