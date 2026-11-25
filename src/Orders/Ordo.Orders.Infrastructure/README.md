# Ordo.Orders.Infrastructure
**Purpose:** Adapts external infrastructure to the Orders ports.
**Layer:** Infrastructure.
**May reference:** `Ordo.Orders.Application`, `Ordo.Orders.Domain`, `Ordo.Contracts`, any `BuildingBlocks.*`, EF Core/Npgsql and `Microsoft.Extensions.*`, per `docs/architecture.md` §4.
**Contains:** `OrdersDbContext`, `IEntityTypeConfiguration<Order>`, EF Core repository/`IUnitOfWork` adapters, `AddOrdersInfrastructure(...)` and the initial migration under `Persistence/Migrations`.
**Does NOT contain:** HTTP endpoints, business rules or another service's code (see [AGENTS.md](../../../AGENTS.md) §6).
**How to test:** `dotnet test Ordo.slnx -c Release --no-build`; persistence behaviour is covered by the Phase 1 end-to-end tests.
**Status:** Phase 1 persistence implemented. One line per product per order is enforced by the domain and the `order_lines` primary key. No TODO(phase-N) here.
