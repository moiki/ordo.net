# Ordo.Orders.Domain
**Purpose:** Owns the Orders service domain model and its rules.
**Layer:** Domain.
**May reference:** Nothing outside the BCL (no ProjectReference/PackageReference today).
**Contains:** `Order` aggregate with the `Pending → AwaitingPayment → Confirmed / Cancelled` state machine, `OrderLine`, `Money`, `OrderStatus`, domain events and `InvalidOrderStateException`.
**Does NOT contain:** EF Core, RabbitMQ, ASP.NET or other services (see [AGENTS.md](../../../AGENTS.md) §6).
**How to test:** `dotnet test Ordo.slnx -c Release --no-build` from the repo root, including `tests/Ordo.Orders.Tests`.
**Status:** Phase 1 state machine and domain events implemented. No TODO(phase-N) here.
