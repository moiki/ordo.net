# 0003. Hand-built outbox and inbox

- Status: Proposed
- Date: 2026-11-24

## Context

The platform requires at-least-once delivery, idempotent consumers, and atomic state-change/event publication. These concerns are central to the learning goals of the project. Messaging frameworks can provide outbox/inbox features, but they also hide important mechanics.

Alternatives considered:
- MassTransit/Wolverine/NServiceBus/Rebus/CAP outbox and inbox features.
- CDC-based publication from the start.
- Hand-built outbox and inbox in building blocks.

## Decision

Implement outbox and inbox manually during the core phases. `Ordo.BuildingBlocks.Messaging.EfCore` does not exist in Phase 0; it will be created in Phase 2. Phase 1 intentionally starts with direct publish to expose the dual-write problem; Phase 2 adds the outbox (same-transaction write, `SKIP LOCKED` relay, publisher confirms, attempts/dead state and retention) and the inbox (consumer deduplication) exactly as specified in [`docs/messaging.md`](../messaging.md) §§5–6, without changing application contracts.

## Consequences

- Positive: deep understanding of reliability mechanics, explicit trade-offs, and no framework lock-in in core code.
- Negative: more implementation and testing effort; higher risk of subtle bugs if tests are weak.
- Revisit: roadmap extras may compare hand-built implementation against a library in an isolated spike and ADR.
