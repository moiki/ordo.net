# Ordo.Messaging.Abstractions.Tests
**Purpose:** Unit tests for the shared envelope and event-body serialization rules.
**Layer:** Tests.
**May reference:** `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions`, and test libraries, per [docs/architecture.md](../../docs/architecture.md) §4.
**Contains:** Tests asserting that `IIntegrationEvent` metadata is carried in headers and never in the JSON body (ADR 0006).
**Does NOT contain:** Broker, database or ASP.NET Core dependencies.
**How to test:** `dotnet test Ordo.slnx -c Release --no-build` from the repo root.
**Status:** Active. No TODO(phase-N) here.
