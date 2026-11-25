# 0002. EDA with broker behind ports

- Status: Proposed
- Date: 2026-11-24

## Context

Ordo services collaborate through integration events. The project starts with RabbitMQ but roadmap Phase 6 requires a Kafka adapter. Business/application code must not become coupled to broker-specific concepts such as exchanges, partitions, offsets, or delivery APIs.

Alternatives considered:
- Use RabbitMQ client APIs directly from application/infrastructure code in each service.
- Use a messaging framework that abstracts the broker.
- Define broker ports in building blocks and implement adapters per broker.

## Decision

Adopt event-driven architecture across services, with broker access hidden behind messaging ports in `Ordo.BuildingBlocks.Messaging.Abstractions`. RabbitMQ is the first adapter. Application code depends on messaging abstractions and integration event contracts, not on RabbitMQ types.

## Consequences

- Positive: broker migration path is explicit, application code remains portable, and messaging concerns are testable through ports.
- Negative: initial implementation requires more deliberate design than direct broker usage; abstractions can leak if not guarded by tests and review.
- Revisit: after Phase 6 Kafka migration, document abstraction leaks/costs and decide whether the ports need adjustment.
