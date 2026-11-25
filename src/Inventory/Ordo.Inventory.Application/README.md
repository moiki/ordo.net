# Ordo.Inventory.Application
**Purpose:** Hosts Inventory use cases and the ports they depend on.
**Layer:** Application.
**May reference:** `Ordo.Inventory.Domain`, `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions`.
**Contains:** `AssemblyMarker.cs`; no commands, queries or handlers yet.
**Does NOT contain:** EF Core, RabbitMQ, ASP.NET or mediator libraries (see [AGENTS.md](../../../AGENTS.md) §6 and §3).
**How to test:** No tests yet; `dotnet test Ordo.slnx -c Release --no-build` from the repo root checks the dependency rules.
**Status:** Empty skeleton with assembly marker only. No TODO(phase-N) here.
