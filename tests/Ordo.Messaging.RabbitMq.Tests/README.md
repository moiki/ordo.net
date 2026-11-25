# Ordo.Messaging.RabbitMq.Tests
**Purpose:** Integration tests for the RabbitMQ adapter against a real broker.
**Layer:** Tests.
**May reference:** `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions`, `Ordo.BuildingBlocks.Messaging.RabbitMq` and test libraries, per [docs/architecture.md](../../docs/architecture.md) §4.
**Contains:** Tests for confirms, persistence, quorum topology, routing key, envelope headers, manual ack, transient requeue and permanent no-requeue.
**Does NOT contain:** Business logic; Docker is required for these tests.
**How to test:** `dotnet test Ordo.slnx -c Release --no-build` from the repo root.
**Status:** Active. No TODO(phase-N) here.
