using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PublicSafetyHub.Core.Entities;

namespace PublicSafetyHub.Data.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<ArticleEntity>
{
    public void Configure(EntityTypeBuilder<ArticleEntity> builder)
    {
        builder.ToTable("Articles");
        builder.HasQueryFilter(article => !article.IsDeleted);
        builder.Property(article => article.Title).IsRequired().HasMaxLength(400);
        builder.Property(article => article.Content).IsRequired();
        builder.Property(article => article.Type).HasConversion<string>().IsRequired().HasMaxLength(32);
        builder.HasIndex(article => new { article.CreatedAt, article.Id });
        builder.HasIndex(article => new { article.Type, article.CreatedAt, article.Id });
    }
}
