# 0004. .NET 10 LTS as initial target

- Status: Accepted
- Date: 2026-11-24

## Context
.NET 11 was released on 2026-11-10 as a Standard Term Support (STS) release. .NET 10 (LTS) and .NET 11 (STS) both reach end of support on 2028-11-14, so support length is not a differentiator for the next two years.

Ordo depends heavily on PostgreSQL through EF Core and the Npgsql provider. At the time of this decision a stable Npgsql EF Core provider for version 11 could not be confirmed (only previews were found). The project's learning goals (outbox, inbox, sagas, resilience, observability) do not require any .NET 11 feature. The developer already has the .NET 10 SDK installed.

Alternatives considered:
- Start on .NET 11 immediately.
- Start on .NET 10 and migrate later.

## Decision
Target `net10.0` for every project, with a .NET 10 SDK pinned in `global.json`. The target framework is defined once in `Directory.Build.props` and package versions once in `Directory.Packages.props`, which keeps a later migration to be a small, mechanical change.

At the end of Phase 2 the team re-evaluates a migration to .NET 11, based on stable support from the Npgsql EF Core provider, `RabbitMQ.Client`, OpenTelemetry and Testcontainers. A migration, if done, is recorded in a new ADR that supersedes this one.

## Consequences
- Positive: ecosystem maturity and stability for the first phases; no early-adopter friction on the critical persistence dependency; the later migration doubles as an exercise in protecting the architecture with tests.
- Negative: C# 15 and .NET 11 features are not available at first.
- PostgreSQL 16 or later is used in local infrastructure so that moving to the Npgsql EF Core 11 provider (which assumes PostgreSQL 16+ by default) requires no database change.
- Revisit: end of Phase 2, or earlier if a needed dependency drops support for `net10.0`.
