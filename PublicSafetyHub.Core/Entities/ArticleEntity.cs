using Pgvector;
using PublicSafetyHub.Base.Entities.Base;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Core.Entities;

public class ArticleEntity : ExtendedBaseEntity<int>
{
    public string Title { get; set; }

    public string Content { get; set; }

    public Vector Embedding { get; set; }

    public string OriginUrl { get; set; }

    public DateTime? PublishedAt { get; set; }

    public ArticleType Type { get; set; }
}
