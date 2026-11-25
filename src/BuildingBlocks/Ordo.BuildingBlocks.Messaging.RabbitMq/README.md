# Ordo.BuildingBlocks.Messaging.RabbitMq
**Purpose:** RabbitMQ adapter for the broker-agnostic messaging ports.
**Layer:** BuildingBlocks (technical infrastructure only).
**May reference:** `Ordo.BuildingBlocks.Messaging.Abstractions`, `RabbitMQ.Client` 7.x and `Microsoft.Extensions.Logging.Abstractions`, per [docs/architecture.md](../../../docs/architecture.md) §4.
**Contains:** Topic exchange `ordo.events`, quorum queues `<service>.<purpose>`, persistent messages, publisher confirms, manual acks, configurable prefetch, envelope header mapping and a hosted consumer per subscription. `ConsumerSubscription.For<TEvent>` dispatches through `IIntegrationEventHandler<TEvent>`; no reflection or in-process bus.
**Does NOT contain:** Business logic, retry queues, DLQ or Inbox. Phase 1 nacks permanent failures with `requeue: false` (`TODO(phase-2)`) and requeues only transient failures; there is no Inbox yet, so handlers must be naturally idempotent.
**How to test:** `dotnet test Ordo.slnx -c Release --no-build`; adapter integration tests use Testcontainers with RabbitMQ 4.4.1.
**Status:** Phase 1 adapter. No other TODO(phase-N) than the DLQ noted above.
