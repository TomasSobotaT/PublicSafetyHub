using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PublicSafetyHub.Core.Entities;

namespace PublicSafetyHub.Data.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<ArticleEntity>
{
    public void Configure(EntityTypeBuilder<ArticleEntity> builder)
    {
        builder.HasQueryFilter(article => !article.IsDeleted);
        builder.Property(article => article.Title).IsRequired().HasMaxLength(400);
        builder.Property(article => article.Content).IsRequired();
        builder.Property(article => article.Embedding).HasColumnType("vector(1536)").IsRequired(false);
        builder.Property(article => article.OriginUrl).HasMaxLength(2048);
        builder.HasIndex(article => article.OriginUrl).IsUnique();
        builder.Property(article => article.Type).HasConversion<string>().HasMaxLength(32);
    }
}
