# Ordo.Inventory.Infrastructure
**Purpose:** Adapts external infrastructure to the Inventory ports.
**Layer:** Infrastructure.
**May reference:** `Ordo.Inventory.Application`, `Ordo.Inventory.Domain`, `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions` (current csproj; per `docs/architecture.md` §4).
**Contains:** `InventoryDbContext`, EF configurations for `stock_items`, `reservations` and `reservation_lines`, EfCore repositories, a UoW that translates row-version conflicts to `SaveOutcome`, and the `InitialCreate` migration.
**Does NOT contain:** HTTP endpoints or another service's code (see [AGENTS.md](../../../AGENTS.md) §6).
**How to test:** `dotnet test Ordo.slnx -c Release --no-build`; migration applied explicitly with `dotnet ef database update`.
**Status:** Phase 1 persistence implemented. `StockItem.Version` maps to PostgreSQL `xmin` via `IsRowVersion()` (verified against Npgsql docs). No TODO(phase-N) here.
