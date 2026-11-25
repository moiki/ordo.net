# Ordo.Orders.Tests
**Purpose:** Unit tests for the Orders aggregate and its application handlers.
**Layer:** Tests.
**May reference:** `Ordo.Orders.Domain`, `Ordo.Orders.Application`, `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions` and test libraries, per [docs/architecture.md](../../docs/architecture.md) §4.
**Contains:** Order state-machine tests (including invalid transitions and idempotent duplicates) and handler tests with fake ports.
**Does NOT contain:** EF Core, RabbitMQ, HTTP or Testcontainers; those belong to later slices.
**How to test:** `dotnet test Ordo.slnx -c Release --no-build` from the repo root.
**Status:** Active. No TODO(phase-N) here.
