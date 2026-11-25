# Ordo.Orders.Infrastructure
**Purpose:** Adapts external infrastructure to the Orders ports.
**Layer:** Infrastructure.
**May reference:** `Ordo.Orders.Application`, `Ordo.Orders.Domain`, `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions` (current csproj; per `docs/architecture.md` §4).
**Contains:** `AssemblyMarker.cs`; no DbContext, repository or adapter yet.
**Does NOT contain:** HTTP endpoints or another service's code (see [AGENTS.md](../../../AGENTS.md) §6).
**How to test:** No tests yet; `dotnet test Ordo.slnx -c Release --no-build` from the repo root checks the dependency rules.
**Status:** Empty skeleton with assembly marker only. No TODO(phase-N) here.
