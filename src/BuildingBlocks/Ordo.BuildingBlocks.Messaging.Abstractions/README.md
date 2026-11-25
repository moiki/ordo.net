# Ordo.BuildingBlocks.Messaging.Abstractions
**Purpose:** Defines broker-agnostic messaging ports for application code.
**Layer:** BuildingBlocks.
**May reference:** `Ordo.Contracts` only, per [docs/architecture.md](../../../docs/architecture.md) §4 (no packages).
**Contains:** `IBrokerPublisher.cs`, `IIntegrationEventHandler.cs`, `IIntegrationEventPublisher.cs`; ports use `IIntegrationEvent` from `Ordo.Contracts`.
**Does NOT contain:** RabbitMQ/EF Core code or business concepts (see [docs/architecture.md](../../../docs/architecture.md) §4).
**How to test:** No project-specific tests yet; `dotnet test Ordo.slnx -c Release --no-build` from the repo root.
**Status:** Interfaces only; no implementations here. No TODO(phase-N) in this project.
