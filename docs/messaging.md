# Messaging

Rules for integration events, the broker, the outbox, the inbox, retries and tracing. The goal is a design where **RabbitMQ can be replaced by Kafka by writing a new adapter**, without touching Domain or Application.

## 1. Delivery guarantees (the mental model)

- The system is **at-least-once**. Exactly-once is not assumed anywhere.
- Duplicates can happen (relay crash after publish, redelivery after missing ack). Therefore **every consumer is idempotent**.
- Global ordering does not exist. Ordering only matters **per aggregate** (see Partition key).
- Events are **facts in the past tense** (`OrderCreated`), not commands (`ReserveStock`).

## 2. Integration event contract

All events live in `Ordo.Contracts`, grouped by owning service:

```
Ordo.Contracts/
├── Orders/    OrderCreatedV1.cs, OrderConfirmedV1.cs, OrderCancelledV1.cs
├── Inventory/ StockReservedV1.cs, StockRejectedV1.cs, StockReleasedV1.cs
└── Payments/  PaymentCompletedV1.cs, PaymentFailedV1.cs
```

Rules:
- Immutable `record`, public properties, no logic, no behavior.
- Class name ends in `V<n>`. The **wire type** is a stable string, independent of the .NET type name: `orders.order-created.v1`.
- Each event class declares its wire type and partition key through `IIntegrationEvent`, which is **defined in `Ordo.Contracts`** (events must implement it, and `Ordo.Contracts` has no dependencies). `Messaging.Abstractions` references `Ordo.Contracts` to use it. Whether it exposes `EventType` and `PartitionKey` as members or through an attribute plus registry is decided in an ADR during Phase 1.
- Types allowed in payloads: `Guid`, `string`, `int`, `long`, `bool`, `DateTimeOffset` (UTC), enums serialized as **strings**, nested records, and lists of records.
- Money: `long AmountMinor` + `string Currency` (ISO 4217). Never floating point.
- Serialization: `System.Text.Json`, camelCase, one shared options instance in `Messaging.Abstractions`.
- Contract evolution:
  - Within a version, only **additive, optional** changes (new nullable property).
  - Removing, renaming, or changing the meaning or type of a property is **breaking** and requires a new `V2` event type, published alongside `V1` until all consumers migrate.
  - Consumers must ignore unknown properties.

### Envelope (metadata carried with every message)

| Field | Description |
|---|---|
| `message_id` | UUID, unique per message. Use `Guid.CreateVersion7()` |
| `type` | Wire type, e.g. `orders.order-created.v1` |
| `occurred_on_utc` | When the fact happened |
| `correlation_id` | Identifies the whole business flow (the saga). Set at the entry point and propagated |
| `causation_id` | `message_id` of the message that caused this one |
| `traceparent` | W3C trace context, for distributed tracing |
| `partition_key` | The aggregate id (e.g. `orderId`). Used for ordering and, in Kafka, as the message key |

The body is the JSON of the event. Metadata travels in broker headers (RabbitMQ headers / properties, later Kafka headers).

### Initial event catalog (changes require an ADR)

| Event (wire type) | Published by | Key fields | Consumed by |
|---|---|---|---|
| `orders.order-created.v1` | Orders | `orderId`, `customerId`, `lines[{productId, quantity, unitPriceMinor}]`, `totalMinor`, `currency` | Inventory |
| `inventory.stock-reserved.v1` | Inventory | `orderId`, `reservationId`, `lines[{productId, quantity}]`, `totalMinor`, `currency` | Orders, Payments |
| `inventory.stock-rejected.v1` | Inventory | `orderId`, `reason`, `unavailableProductIds` | Orders |
| `payments.payment-completed.v1` | Payments | `orderId`, `paymentId` | Orders |
| `payments.payment-failed.v1` | Payments | `orderId`, `paymentId`, `reason` | Orders |
| `orders.order-confirmed.v1` | Orders | `orderId`, `customerId` | Inventory (commit reservation), Notifications |
| `orders.order-cancelled.v1` | Orders | `orderId`, `customerId`, `reason` | Inventory (release stock), Notifications |
| `inventory.stock-released.v1` | Inventory | `orderId`, `reservationId` | (observability / future use) |

Notes:
- `PaymentFailed` is intentionally **not** consumed by Notifications: Orders reacts by cancelling, and `OrderCancelled` (with its reason) triggers the single customer notification. This avoids duplicate emails.
- Saga happy path: `OrderCreated → StockReserved → PaymentCompleted → OrderConfirmed`.
- Compensation: `PaymentFailed → OrderCancelled → Inventory releases stock (StockReleased)`.

## 3. Ports in `Messaging.Abstractions`

Keep these **small**. Resist the urge to build a generic bus.

- (`IIntegrationEvent` itself lives in `Ordo.Contracts`; these ports use it.)
- `IIntegrationEventPublisher` — used by Application. `PublishAsync(IIntegrationEvent, CancellationToken)`.
  - Phase 1 implementation: publishes directly to RabbitMQ.
  - Phase 2 implementation: writes to the outbox (same DbContext/transaction). Application code does not change.
- `IIntegrationEventHandler<TEvent>` — implemented in Application, invoked by the consumer adapter.
- `IBrokerPublisher` — **infrastructure-level**, used by the outbox relay to actually send to the broker. Implemented by `Messaging.RabbitMq` (and later `Messaging.Kafka`).
- A message envelope/headers type shared by adapters.

### Portability rules (so Kafka remains possible)

Application and Domain code must **not** rely on:
- RabbitMQ exchanges, routing keys, queue names, or topology concepts.
- Per-message TTL, priorities, or `reply-to` / RPC over the broker.
- Broker redelivery counters or `x-death` headers. Retry accounting is the adapter's concern.
- Total ordering across aggregates.
- Messages being deleted after consumption (Kafka retains and can replay; handlers must tolerate replays).

## 4. RabbitMQ topology (adapter detail, `Messaging.RabbitMq` only)

Use `RabbitMQ.Client` 7.x async API. One long-lived connection per process, and channels per publisher/consumer (channels are not thread-safe; follow the official guidance for the 7.x API).

- Exchange: one durable **topic** exchange `ordo.events`. Routing key = wire type (e.g. `orders.order-created.v1`).
- Queues: **one queue per consumer purpose**, named `<service>.<purpose>` (e.g. `inventory.on-order-created`). Durable, **quorum** type. Bound to `ordo.events` with the routing keys it consumes.
- Messages are published as **persistent**, with `mandatory` handling and **publisher confirms** enabled. Treat an unconfirmed or returned message as a publish failure.
- Consumers use **manual acknowledgements** and a configurable `prefetch` (QoS). Never auto-ack.
- Each service declares its own queues and bindings at startup (idempotent declare). A service never declares another service's queues.

### Retry and dead-lettering

- Failures are classified:
  - **Permanent** (deserialization failure, unknown type, contract violation, business rule that will never succeed): do not retry, send straight to the parking-lot queue.
  - **Transient** (DB unavailable, concurrency conflict, timeout): retry with backoff.
- Retry mechanism: tiered **retry queues** `<queue>.retry.<n>` with a message TTL per tier (e.g. a few seconds, then 30 s, then a few minutes; make the tiers configurable). When the TTL expires the message is dead-lettered **back to the originating queue**.
- **Critical:** retry queues must dead-letter through the **default exchange** with routing key = originating queue name. Do **not** dead-letter back to `ordo.events`, otherwise every other subscriber bound to that routing key receives the message again.
- Attempt counting uses an application header such as `x-ordo-attempt`, incremented by the adapter. Do not rely on `x-death`.
- After the maximum number of attempts, publish to the parking-lot queue `<queue>.dlq` with the failure reason and original headers. DLQs are inspected and replayed manually (a replay tool is a later extra).
- Handlers and the adapter must log: message id, type, correlation id, attempt number, and outcome.

## 5. Outbox (Phase 2, hand-built, PostgreSQL + EF Core)

Lives in `Ordo.BuildingBlocks.Messaging.EfCore`. Each service has its **own** `outbox_messages` table **in its own database**.

```sql
CREATE TABLE outbox_messages (
    id                    uuid        PRIMARY KEY,   -- = message_id
    type                  text        NOT NULL,      -- wire type, NOT a .NET type name
    payload               jsonb       NOT NULL,
    headers               jsonb       NULL,          -- correlation_id, causation_id, traceparent, partition_key
    occurred_on_utc       timestamptz NOT NULL,
    processed_on_utc      timestamptz NULL,
    dead_lettered_on_utc  timestamptz NULL,
    attempts              int         NOT NULL DEFAULT 0,
    last_error            text        NULL
);

CREATE INDEX ix_outbox_pending
    ON outbox_messages (occurred_on_utc)
    WHERE processed_on_utc IS NULL AND dead_lettered_on_utc IS NULL;
```

### Writing
- The outbox row is added to the **same `DbContext` and transaction** as the business change. Atomicity is the entire point.
- Capture `traceparent` and `correlation_id` at write time and store them in `headers`.

### Relay (polling publisher, a `BackgroundService`)
- Each cycle, inside a transaction, select a batch with:
  ```sql
  SELECT ... FROM outbox_messages
  WHERE processed_on_utc IS NULL AND dead_lettered_on_utc IS NULL
  ORDER BY occurred_on_utc
  LIMIT @batchSize
  FOR UPDATE SKIP LOCKED;
  ```
  This allows multiple relay instances without double-processing. Use raw SQL (EF Core raw SQL or Npgsql). If unsure how to do it correctly, ask.
- Publish through `IBrokerPublisher` and **wait for the broker confirm**. Only after the confirm, set `processed_on_utc`.
- On failure: increment `attempts`, set `last_error`. After `MaxAttempts`, set `dead_lettered_on_utc` (the outbox's own dead-letter state) and log loudly.
- Do **not** use an increasing id or timestamp as a cursor ("give me everything after X"): a slow transaction can commit an older row after the cursor moved and the message is lost. The `processed_on_utc IS NULL` flag is the source of truth.
- Restore the stored `traceparent` as the parent when publishing so traces are not broken.
- Batch size, poll interval and max attempts are configuration, not constants.
- Graceful shutdown: finish or roll back the current batch, honor `CancellationToken`.
- A **retention job** deletes processed rows older than a configurable age.
- Ordering: the relay publishes in `occurred_on_utc` order, but strict per-aggregate ordering across multiple relay instances is **not guaranteed**. Consumers must be tolerant (check aggregate state, ignore stale transitions). Document any case where this matters.

## 6. Inbox (Phase 2, hand-built)

Each consuming service has an `inbox_messages` table in its own database:

```sql
CREATE TABLE inbox_messages (
    message_id       uuid        NOT NULL,
    consumer         text        NOT NULL,   -- handler/consumer name
    processed_on_utc timestamptz NOT NULL,
    PRIMARY KEY (message_id, consumer)
);
```

Consumer flow:
1. Deserialize and validate the message.
2. Open a DB transaction.
3. Insert `(message_id, consumer)` into the inbox. On unique violation → this is a duplicate: **ack and skip**.
4. Run the handler (business change + any outbox rows) in the **same transaction**.
5. Commit, then **ack** the broker message.

If the process dies after commit but before ack, the message is redelivered and step 3 detects the duplicate. That is the intended behavior.

Handlers should also be naturally idempotent where possible (e.g. "reserve stock for order X" checks if a reservation for X already exists).

## 7. Observability across messages

- Propagate W3C trace context (`traceparent`) through the outbox and broker headers. Consumers start a **new span linked to or parented by** the producer's context.
- Use dedicated `ActivitySource`s for publish and consume, following OpenTelemetry messaging semantic conventions (verify the current conventions, do not invent attribute names).
- `correlation_id` is included in every log scope.
- Never log full payloads containing personal data; log ids and types.

## 8. Testing rules for messaging

- Unit tests for handlers use fakes of the ports; no broker involved.
- Integration tests use Testcontainers (RabbitMQ + PostgreSQL) and must cover, from Phase 2:
  - Duplicate delivery produces a single business effect.
  - A message published while the broker is down is delivered after recovery (outbox).
  - A permanently failing message ends in the DLQ; a transiently failing one is retried and then succeeds.
- Contract tests (Phase 5) assert the JSON shape of each event version to catch accidental breaking changes.
- Do not use `Task.Delay` as synchronization in tests. Poll with a timeout and a clear failure message.

## 9. Kafka readiness checklist (Phase 6)

When writing the Kafka adapter you should only need to:
- Implement `IBrokerPublisher` and the consumer host with the same envelope semantics.
- Map `partition_key` to the Kafka message key, wire type to a topic or header, envelope fields to headers.
- Implement retry via retry topics or a pause/backoff strategy inside the adapter.
- Keep the Inbox (it remains necessary: Kafka consumers are also at-least-once).

If any change is required in Domain or Application, the abstraction leaked. Record what leaked in an ADR.
