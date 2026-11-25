# Ordo.Inventory.Tests
**Purpose:** Unit tests for the Inventory domain and application handlers.
**Layer:** Tests.
**May reference:** `Ordo.Inventory.Domain`, `Ordo.Inventory.Application`, `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions`, xUnit, Shouldly.
**Contains:** Tests for `StockItem`, `Reservation`, `OrderCreatedHandler` and `SetStockHandler`, plus in-memory fake ports.
**Does NOT contain:** EF Core, RabbitMQ, real databases or other services' code.
**How to test:** `dotnet test Ordo.slnx -c Release --no-build` from the repo root.
**Status:** Phase 1 rules covered, including duplicate-delivery idempotency and row-version conflict retries. No TODO(phase-N) here.
