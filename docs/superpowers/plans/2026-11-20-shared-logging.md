# Shared logging implementation plan

**Goal:** Implement the user-approved reusable Base exception handler and Serilog logging to PostgreSQL, with application Warning+ and other sources Error+.

**Architecture:** Services retain Microsoft ILogger<T>. Base owns reusable exception handling, file logging and PostgreSQL mapping. Host supplies configuration and connects the components. No request status/timing logging and no custom ILogService.

**Tech stack:** .NET 10, Serilog.AspNetCore 10, Serilog.Sinks.Postgresql.Alternative, PostgreSQL.

**Spec:** Approved design in this conversation: one Logs table, UTC timestamps, structured properties, independent batched database writes, rotating files, configurable 30-day retention and eventual admin filtering.

## Constraints and rulings

- Do not add tests or testing dependencies; verify through build and code review (AGENTS.md).
- Keep existing project conventions and C# 14 extension blocks.
- Work in the current checkout: sandbox denied git worktree creation; initial status was clean.
- Execute directly under the user's instruction to implement the agreed design; no additional approval gate.
- Do not create a speculative Blazor UI. Provide authenticated admin read endpoints if the existing authorization model can support them safely.
- Do not commit, reset, delete the application database or expose secret configuration during verification.

## Tasks

- [x] Add Base logging options, common PostgreSQL columns, validated sink registration and GlobalExceptionHandler returning safe ProblemDetails with trace ID.
- [x] Wire Host startup logging, shutdown flushing and exception middleware; replace old Logging settings, configure retention and outputs.
- [x] Add shared log read model/schema and project-specific admin filtering using existing service/repository patterns; document schema provisioning and persistence across development database recreation.
- [x] Build backend/solution where workloads permit, review diffs and record verification limits.

## Review focus

- PostgreSQL unavailable: independent file output remains; bounded DB queue and sink diagnostics.
- Failed startup: bootstrap logger records failure and flushes; credentials stay out of messages.
- Exceptions already logged by a library: do not globally suppress all framework errors; .NET 10 handler diagnostics avoid the immediate middleware duplicate.
- UTC types and nullable trace ID outside HTTP; stable database identity independent of business entities.
- Admin reads must never be anonymous, must paginate/filter in SQL, and must not return exception details on the list.

## Execution evidence

- Base and Host builds passed. Full solution build passed: `dotnet build PublicSafetyHub.slnx -p:StartDevelopmentDatabase=false --verbosity minimal`, exit 0, 0 errors, 12 preexisting AutoMapper NU1903 warnings (restore/build repeats advisories).
- No tests were added or run. The application was not started, so no application database reset or live logging writes were performed. The first Host build invoked the repository's existing Docker startup build target; subsequent builds disable that target.
- Reviewed the sink's TimestampColumnWriter source: it converts timestamps to UTC. Used the built-in writer so sink retention can identify the timestamp column.
- Added narrowly scoped gitignore exceptions for the logging documentation/plan; log files remain ignored. Models use Logging directory to avoid the existing global Logs directory exclusion.
- Independent review completed: no important findings in sink types/defaults, retention, startup/failure paths, SQL reads or authorization. Runtime persistence and authorized access remain unverified.
