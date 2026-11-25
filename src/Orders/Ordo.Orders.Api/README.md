# Ordo.Orders.Api
**Purpose:** Composition root and HTTP inbound adapter for Orders.
**Layer:** Api.
**May reference:** `Ordo.Orders.Application`, `Ordo.Orders.Infrastructure`, `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions` (current csproj; per `docs/architecture.md` §4).
**Contains:** `Program.cs` (`/health/live`, `/health/ready`), `AssemblyMarker.cs`, `appsettings*.json`, `Ordo.Orders.Api.http`, `Properties/launchSettings.json`.
**Does NOT contain:** Business logic or another service's code (see [AGENTS.md](../../../AGENTS.md) §6).
**How to test:** No endpoint tests yet; `dotnet test Ordo.slnx -c Release --no-build` from the repo root.
**Status:** Health endpoints only. No TODO(phase-N) in this project.
