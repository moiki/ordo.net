# Ordo.Inventory.Application
**Purpose:** Hosts Inventory use cases and the ports they depend on.
**Layer:** Application.
**May reference:** `Ordo.Inventory.Domain`, `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions`.
**Contains:** `OrderCreatedHandler` (reserve or reject, duplicate-safe), `SetStockHandler` (dev admin), the repository/UoW ports, `SaveOutcome`, `Result`/`Error` and the mapping to `StockReservedV1` / `StockRejectedV1`.
**Does NOT contain:** EF Core, RabbitMQ, ASP.NET or mediator libraries (see [AGENTS.md](../../../AGENTS.md) §6 and §3).
**How to test:** `dotnet test Ordo.slnx -c Release --no-build`; handler tests use in-memory fake ports.
**Status:** Phase 1 handlers implemented. Idempotency comes from the persisted decision per OrderId, not an Inbox; that arrives in Phase 2. No TODO(phase-N) here.
