# Ordo.Inventory.Domain
**Purpose:** Owns the Inventory service domain model and its rules.
**Layer:** Domain.
**May reference:** Nothing outside the BCL (no ProjectReference/PackageReference today).
**Contains:** `StockItem` (available quantity plus an `xmin` version token), `Reservation` with `ReservationLine` and `ReservationStatus`, and the `ReservationPlaced` / `ReservationRejected` domain events.
**Does NOT contain:** EF Core, RabbitMQ, ASP.NET or other services (see [AGENTS.md](../../../AGENTS.md) §6).
**How to test:** `dotnet test Ordo.slnx -c Release --no-build`; `tests/Ordo.Inventory.Tests` covers the invariants directly.
**Status:** Phase 1 reservation rules implemented. No TODO(phase-N) here.
