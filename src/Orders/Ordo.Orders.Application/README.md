# Ordo.Orders.Application
**Purpose:** Hosts Orders use cases and the ports they depend on.
**Layer:** Application.
**May reference:** `Ordo.Orders.Domain`, `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions`.
**Contains:** `CreateOrderHandler`, `GetOrderHandler`, `StockReservedHandler`, `StockRejectedHandler`, command/query DTOs, `IOrderRepository`, `IUnitOfWork`, `Result<T>`/`Error` and the explicit domain-to-integration event mapper.
**Does NOT contain:** EF Core, RabbitMQ, ASP.NET or mediator libraries (see [AGENTS.md](../../../AGENTS.md) §6 and §3).
**How to test:** `dotnet test Ordo.slnx -c Release --no-build` from the repo root, including `tests/Ordo.Orders.Tests`.
**Status:** Phase 1 use cases implemented. `CreateOrderHandler` publishes after commit (documented dual-write flaw, fixed in Phase 2). No other TODO(phase-N) here.
