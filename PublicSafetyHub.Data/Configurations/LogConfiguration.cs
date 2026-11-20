using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PublicSafetyHub.Core.Entities;

namespace PublicSafetyHub.Data.Configurations;

public class LogConfiguration : IEntityTypeConfiguration<LogEntity>
{
    public const string SchemaName = "logging";

    public const string TableName = "Logs";

    public void Configure(EntityTypeBuilder<LogEntity> builder)
    {
        builder.ToTable(TableName, SchemaName);
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Timestamp).HasColumnType("timestamp with time zone");
        builder.Property(log => log.MessageTemplate).IsRequired();
        builder.Property(log => log.Message).IsRequired();
        builder.Property(log => log.Properties).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(log => new { log.Timestamp, log.Id }).IsDescending(true, true).HasDatabaseName("IX_Logs_Timestamp_Id");
        builder.HasIndex(log => new { log.Level, log.Timestamp }).IsDescending(false, true).HasDatabaseName("IX_Logs_Level_Timestamp");
        builder.HasIndex(log => log.TraceId).HasDatabaseName("IX_Logs_TraceId");
    }
}
