# Phase 0 Status — Foundations

**Date:** 2026-11-24  
**Branch:** `main`  
**Commit:** `fda3053` (`first commit`)  
**Remote:** `git@github_moiki:moiki/ordo.net.git`

## Summary

Phase 0 foundations initialized for Ordo (.NET 10, hexagonal EDA skeleton) following `AGENTS.md` and `docs/*`.

## Completed

- **Docs normalized**: `DOCS/` → `docs/`; ADRs 0004 and 0005 moved to `docs/adr/` (content preserved, not rewritten). Added `docs/adr/template.md`.
- **ADR drafts (Proposed, not Accepted)**:
  - `docs/adr/0001-monorepo-with-single-slnx.md`
  - `docs/adr/0002-eda-with-broker-behind-ports.md`
  - `docs/adr/0003-hand-built-outbox-and-inbox.md`
- **Solution/configuration**:
  - `Ordo.slnx`
  - `global.json` (SDK `10.0.203`, `rollForward: latestPatch`)
  - `Directory.Build.props` (`net10.0`, nullable, implicit usings, warnings as errors, analyzers, deterministic, Ordo metadata)
  - `Directory.Packages.props` (Central Package Management)
  - `.editorconfig`, `.gitignore`, `README.md`
- **Project skeleton** (empty/compilable, no business logic):
  - `src/Contracts/Ordo.Contracts` (`IIntegrationEvent`)
  - `src/BuildingBlocks/Ordo.BuildingBlocks.Messaging.Abstractions` (`IBrokerPublisher`, `IIntegrationEventPublisher`, `IIntegrationEventHandler<T>`)
  - `src/BuildingBlocks/Ordo.BuildingBlocks.Messaging.RabbitMq` (`RabbitMqBrokerPublisher` with `NotImplementedException("TODO(phase-1): ...")`)
  - `src/BuildingBlocks/Ordo.BuildingBlocks.Messaging.EfCore` (`OutboxMessage`, `InboxMessage` placeholders)
  - Orders/Inventory: `Domain`, `Application`, `Infrastructure`, `Api` with `AssemblyMarker` and minimal APIs exposing `/health/live` and `/health/ready`
- **References**: configured per `docs/architecture.md`; no cross-service references. Payments/Notifications not created (out of Phase 0 scope).
- **Architecture tests**: `tests/Ordo.ArchitectureTests/ArchitectureRulesTests.cs` using NetArchTest.Rules + assembly reference checks. Covers Domain isolation, Application framework restrictions, no cross-service refs, Contracts shape, integration event conventions, and BuildingBlocks technical boundary.
- **Local infra**: `deploy/docker-compose.yml` with PostgreSQL `16.4-alpine`, RabbitMQ `3.13.7-management-alpine` (management UI), Mailpit `v1.21.8`, healthchecks, volumes, dev-only credentials `ordo_dev`.
- **CI**: `.github/workflows/ci.yml` with restore, build Release, test, format check on .NET 10.

## Verification

| Command | Result |
|---|---|
| `dotnet --version` | `10.0.203` |
| `dotnet build Ordo.slnx -c Release` | Pass — 0 warnings, 0 errors |
| `dotnet test Ordo.slnx -c Release --no-build` | Pass — 6 passed, 0 failed |
| `dotnet format Ordo.slnx --verify-no-changes` | Pass |
| `docker compose -f deploy/docker-compose.yml up -d --wait` | Not verified — Docker daemon unavailable in session |

## Forbidden-reference check (Phase 0 requirement)

A temporary forbidden ProjectReference was added to `Ordo.Orders.Domain` and architecture tests were run. The initial type-usage-only check did not catch an unused project reference, so `AssertNoForbiddenDependencies` was reinforced to also inspect `Assembly.GetReferencedAssemblies()`. The temporary forbidden reference was reverted; architecture tests pass in final state.

## Deviations / Notes

- Docker Compose runtime not verified because Docker daemon was unavailable.
- Package versions verified before pinning: `NetArchTest.Rules` 1.3.2, `xunit` 2.9.3, `Shouldly` 4.3.0. Chose `xunit.runner.visualstudio` 2.8.2 for xUnit v2 compatibility; `Microsoft.NET.Test.Sdk` 18.12.0.
- Git initialized and pushed as `first commit` (`fda3053`) to `git@github_moiki:moiki/ordo.net.git`.
- Integration-event architecture rule exists but has no event types yet; it will harden as events are added.
- NetArchTest.Rules 1.3.2 uses `ICustomRule.MeetsRule(Mono.Cecil.TypeDefinition)`; test adjusted accordingly.

## Pending / TODOs

- `TODO(phase-1): publish message to RabbitMQ` in `src/BuildingBlocks/Ordo.BuildingBlocks.Messaging.RabbitMq/RabbitMqBrokerPublisher.cs`.
- Verify Docker Compose with daemon active: `docker compose -f deploy/docker-compose.yml up -d --wait`.
- Human confirmation/acceptance for ADRs 0001-0003 (currently Proposed).
- Add `docs/messaging.md` when messaging phase starts (README references it as future doc).