# Logging

Application services use Microsoft `ILogger<T>`; no custom write service is required. Logging and exception handling are project-specific and are not in Base. Host/BuilderExtensions contains LoggingExtension and ExceptionHandlingExtension. GlobalExceptionHandler and TraceContextEnricher are in Host/Logging. Logging options and interfaces are in Core/Configurations. The entity is Core/Entities/LogEntity.cs, its mapping is Data/Configurations/LogConfiguration.cs and its schema/table names are shared constants on LogConfiguration. Serilog NuGet packages belong to Host. PostgreSQL writes run in batches using a separate connection, independently of application EF transactions.

## Configuration

`Host/appsettings.json` sets Serilog minimum `Error` and overrides the `PublicSafetyHub` category prefix to `Warning`. Therefore application Information/Debug/Trace and library Warning messages are intentionally omitted. Existing logging calls remain in place for future configuration changes. There is no HTTP request/status/duration logging middleware.

`ApplicationLoggingOptions` controls `FilePath`, `RetainedFileCountLimit` (30 files) and `FileSizeLimitBytes` (10 MiB). Files rotate daily and on size. The file count is a count, not a guarantee of 30 days. Paths are relative to the process working directory; use a persistent writable absolute path when deploying. Console output is also enabled. Serilog internal sink failures go to standard error, independently of the logging pipeline.

`PostgreSqlLoggingOptions` controls:

| Setting | Default in Host |
| --- | --- |
| ConnectionStringName | DefaultConnection |
| RetentionDays | 30 |
| BatchSizeLimit | 50 |
| QueueLimit | 10000 |
| PeriodSeconds | 5 |

PublicSafetyHub uses one database and one EF context, PublicSafetyHubDbContext. Application tables and logging.Logs both use DefaultConnection, supplied through user secrets or environment variables. Keep PostgreSqlLoggingOptions:ConnectionStringName set to DefaultConnection so the sink and admin reads target the same database. Do not place production credentials in checked-in settings.

Apply EF migrations to the database before starting the application. The runtime account needs INSERT, SELECT and DELETE privileges for logging, admin reads and retention. Runtime logging does not create schemas, tables or indexes; automatic sink schema/table creation is disabled. LogConfiguration defines the logging table and indexes for EF migrations.

Retention removes old database entries when the sink processes a batch; it is not a scheduled cleanup during an idle application. The bounded in-memory queue is not durable. A sudden process termination can lose queued DB events; an independent file sink records the same admitted messages. Graceful shutdown flushes Serilog. Bootstrap/startup failures also have console/file output. File output is not automatically replayed into PostgreSQL.

**Database lifecycle:** startup no longer calls EnsureDeleted or EnsureCreated. Database changes are managed through EF migrations applied separately; this change does not generate or apply a migration.

## Record and error ownership

Columns: Id (bigint identity), Timestamp (UTC timestamptz), Level (integer), Category, MessageTemplate, Message, Exception, TraceId, Properties (jsonb). Severity values are Trace=0, Debug=1, Information=2, Warning=3, Error=4, Critical=5. These correspond to Serilog Verbose through Fatal. API models use LogSeverity with these values; JSON responses serialize the enum as a number according to existing API defaults, while query filters accept its names. SpanId and logger scopes are included in Properties when present. TraceId is optional outside traced operations.

The global HTTP handler logs unhandled request exceptions and returns a generic 500 ProblemDetails with traceId, without exposing the exception. ASP.NET Core 10 suppresses its own handled-exception diagnostics by default. Locally handled import errors are logged where processing continues; job-level errors are logged at the job boundary. A catch that only rethrows should not log again. Framework/database logs can still describe the same underlying failure from a different layer; they are not globally deduplicated.

Import messages share ImportRunId and Source through a logger scope; embedding runs share EmbeddingRunId. Preserve structured templates, for example `logger.LogWarning("Article {ArticleId} has no content", id)`. Do not log credentials, authorization headers, tokens or complete authentication payloads. No automatic request body or query-string capture is configured.

## Admin API

- `GET /api/v1/admin/logs`
- `GET /api/v1/admin/logs/{id}`

Both require the existing ApiAccess policy and the exact Identity role `Admin`. No account is automatically promoted and this change does not seed an administrator. Ensure the trusted administrator has that role and that a newly issued token includes it through the existing authentication flow.

List requests use existing pagination (PageNumber, PageSize 1–100, SortBy, SortDirection) and FilteredRequest binding. Supported filter fields: From/To (inclusive UTC timestamps with Z suffix), Level (exact LogSeverity), Category (case-sensitive substring), SearchValue (case-sensitive message substring), TraceId (exact). Sort columns: Id, Timestamp, Level, Category. Default is Timestamp descending, with Id as a deterministic tie-breaker following the shared specification convention.

Example: `/api/v1/admin/logs?PageNumber=1&PageSize=20&Filter.Level=Error&Filter.From=2026-11-20T00%3A00%3A00Z`.

List projection excludes exception text, properties and templates. Detail includes those fields; Properties is the stored JSON serialized as a string. All filtering, projection and pagination occur in SQL. Only a read API is exposed; log entries are not editable and do not use business soft deletion. PublicSafetyHubDbContext exposes Logs as DbSet<LogEntity>, with LogConfiguration registered through the existing assembly configuration scan. EF migrations create and update the log table and indexes; there is no manual SQL initializer. Serilog writes bypass EF and UnitOfWork, so an application transaction rollback does not discard its log entries.

API DTO projections are defined in Application/Projections/LogProjection.cs and supplied to generic ILogRepository methods. Core and Data do not reference API Models. LogSeverity is a shared enum in Core/Enums; API Models references Core for enums. Import models and integration repository interfaces are owned by Integration, in IntegrationModels and Repositories/Interfaces respectively.

## Registration and MSSQL

For PostgreSQL, call `builder.AddPostgreSqlLogging()` (includes file/console setup) and `builder.Services.AddGlobalExceptionHandling()` from Host/BuilderExtensions, then `app.UseExceptionHandler()` before request handling. Both logging options sections are required and registered through the existing Base `AddConfiguration<TInterface, TConfiguration>` helper, including validation. Logging consumers resolve `IApplicationLoggingOptions` / `IPostgreSqlLoggingOptions` from DI; they do not bind duplicate instances or silently substitute missing sections. Base retains only its general configuration helper; it contains no logging or exception-handling implementation or Serilog packages.

A future MSSQL implementation replaces PostgreSQL schema/mapping/output and the log read provider. Existing ILogger calls and the exception handler stay unchanged. Migrating the entire application, including pgvector search, is a separate change.

## Verification

Per AGENTS.md no tests or testing dependencies were added. Build command: `dotnet build PublicSafetyHub.Host/PublicSafetyHub.Host.csproj -p:StartDevelopmentDatabase=false`. Runtime database writes, retention and authorized API access require separate integration verification; a successful build does not prove them.
