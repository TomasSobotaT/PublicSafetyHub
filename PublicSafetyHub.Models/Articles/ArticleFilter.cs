using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Models.Articles;

public class ArticleFilter
{
    public ArticleType? Type { get; set; }

    public string SearchValue { get; set; }
}
