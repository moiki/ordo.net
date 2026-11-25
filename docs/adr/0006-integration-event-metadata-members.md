# 0006. Declare wire type and partition key as members of IIntegrationEvent

- Status: Accepted
- Date: 2026-11-25

## Context

`docs/messaging.md` §2 requires every integration event to declare a stable wire type (for example `orders.order-created.v1`) and a partition key (the aggregate id). It leaves the mechanism to an ADR in Phase 1. Two alternatives were considered:

1. Members on `IIntegrationEvent`, implemented by each event record.
2. An attribute plus a reflection-based registry in `Messaging.Abstractions`.

The adapter must read this metadata without reflection or a registry that can drift, and .NET type names must never appear on the wire.

## Decision

Extend `IIntegrationEvent` in `Ordo.Contracts` with `string EventType { get; }` and `string PartitionKey { get; }`. Every versioned event record implements them (`EventType => "orders.order-created.v1"`, `PartitionKey => OrderId.ToString()`). The adapter reads both directly: `EventType` becomes the envelope `type` header **and** the RabbitMQ routing key; `PartitionKey` becomes the envelope `partition_key` header. In RabbitMQ the partition key does not affect routing; it is carried so a future Kafka adapter can use it as the message key.

All four metadata members inherited from `IIntegrationEvent` (`EventId`, `OccurredAtUtc`, `EventType`, `PartitionKey`) are marked `[JsonIgnore]` on the concrete records. They travel in broker headers, never in the JSON body. A unit test asserts that the serialized body contains none of those keys.

## Consequences

- Positive: compile-time enforcement, no reflection, no registry to keep in sync, simple adapter code, straightforward contract tests.
- Negative: each event repeats two members. Adding members to a public interface is a source-breaking change for external implementers; all events currently live in this repository. Changing a wire type string remains a breaking contract change and requires a new `V2` event.
- The attribute-plus-registry alternative is rejected because of reflection and the risk of a missing registration. Revisit only if events come to be defined outside `Ordo.Contracts`.
