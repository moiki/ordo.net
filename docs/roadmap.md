# Roadmap

## Working agreement

- Work on **one phase at a time**. Do not start the next phase until the human says the current one is accepted.
- At the start of each phase: read this file and the relevant docs, then **propose a plan** and wait for approval.
- Work in vertical slices, with a commit per slice.
- A phase is done only when **every "Done when" item is verified** by running commands or tests, and reported.
- Anything listed under "Out of scope" must not be implemented, even if it seems convenient.
- Tick the checkbox below only when the human confirms.

## Status

- [ ] Phase 0: Foundations
- [ ] Phase 1: Orders + Inventory happy path (raw RabbitMQ)
- [ ] Phase 2: Reliability (Outbox, Inbox, retries, DLQ, idempotency)
- [ ] Phase 3: Payments + full saga with compensation
- [ ] Phase 4: Notifications
- [ ] Phase 5: Observability and test suite
- [ ] Phase 6: Kafka adapter
- [ ] Extras

---

## Phase 0: Foundations

**Scope**
- Repo skeleton following the layout in `AGENTS.md`: `Ordo.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`, `README.md`.
- Empty project structure for **Orders** and **Inventory** (Domain, Application, Infrastructure, Api) plus `Ordo.Contracts` and `Ordo.BuildingBlocks.Messaging.Abstractions`. Payments and Notifications are created in their own phases.
- `deploy/docker-compose.yml` with PostgreSQL (16 or later), RabbitMQ (management UI) and Mailpit, with healthchecks.
- `tests/Ordo.ArchitectureTests` implementing the rules from `architecture.md` section 9.
- GitHub Actions workflow: restore, build, test, format check.
- ADR drafts: `0001-monorepo-with-single-slnx`, `0002-eda-with-broker-behind-ports`, `0003-hand-built-outbox-and-inbox`. The human supplies context; the agent drafts; the human accepts.
- ADRs `0004-dotnet-10-lts-as-initial-target` and `0005-cqrs-with-explicit-handlers-and-decorators` **already exist** in `docs/adr/` and are accepted. Do not rewrite them.
- The decorator registration helper described in `architecture.md` may wait until the first handler that needs it (Phase 1).
- `docs/adr/` with the template.

**Done when**
- `dotnet build Ordo.slnx -c Release` succeeds with zero warnings.
- Architecture tests pass and demonstrably **fail** if you add a forbidden reference (verify once, then revert).
- `docker compose -f deploy/docker-compose.yml up -d --wait` brings all containers to healthy.
- CI workflow file is present and valid.

**Out of scope**: any business logic, any messaging code.

---

## Phase 1: Orders + Inventory happy path (raw RabbitMQ)

**Scope**
- **Orders**: `Order` aggregate with status machine (`Pending`, `AwaitingPayment`, `Confirmed`, `Cancelled`), `POST /orders`, `GET /orders/{id}`, EF Core + migrations.
- **Inventory**: `StockItem` and `Reservation`, an admin endpoint to set stock, EF Core + migrations, optimistic concurrency on `StockItem`.
- Events: `OrderCreatedV1`, `StockReservedV1`, `StockRejectedV1` in `Ordo.Contracts`.
- `Ordo.BuildingBlocks.Messaging.RabbitMq`: `IBrokerPublisher` and a consumer host using the topology in `messaging.md` (exchange, quorum queues, manual ack, publisher confirms, prefetch).
- Phase 1 publisher publishes **directly** to RabbitMQ after commit (known dual-write flaw, documented in code comments and the report).
- Orders consumes `StockReserved` (→ `AwaitingPayment`) and `StockRejected` (→ `Cancelled`). Payments does not exist yet, so `AwaitingPayment` is the end of the flow.
- ADR draft: how event type and partition key are declared (see `messaging.md` section 2).

**Done when**
- Integration test (Testcontainers): create stock → create order → order eventually reaches `AwaitingPayment`.
- Integration test: order exceeding stock → order eventually `Cancelled`.
- Concurrency test: two simultaneous orders for the last unit → exactly one reservation succeeds.
- Unit tests cover the `Order` state machine including invalid transitions.
- Architecture tests still pass.

**Out of scope**: outbox, inbox, retries beyond basic nack, payments, notifications.

---

## Phase 2: Reliability

**Scope**
- Outbox in `Ordo.BuildingBlocks.Messaging.EfCore` exactly as specified in `messaging.md` section 5 (table, same-transaction write, `SKIP LOCKED` relay, confirms, attempts/dead state, retention job, trace context).
- Swap the Application-facing `IIntegrationEventPublisher` implementation from "direct" to "outbox". **Application code must not change.**
- Inbox as specified in section 6.
- Retry queues and parking-lot DLQ as specified in section 4, including the default-exchange dead-letter detail.
- `Idempotency-Key` support on `POST /orders`.
- **ADR 0004 checkpoint:** at the end of the phase, report whether a stable `Npgsql.EntityFrameworkCore.PostgreSQL` 11.x, `RabbitMQ.Client`, OpenTelemetry and Testcontainers support `net11.0` (verify with `dotnet package search`; do not guess). Do **not** migrate; the human decides and a new ADR records it.

**Done when**
- Test: kill the publish path (broker down) after a successful business commit → event is delivered after recovery.
- Test: the same message delivered twice → one business effect, second one acked and skipped.
- Test: transient failure is retried and succeeds; permanent failure ends in `<queue>.dlq` with reason.
- Test: two relay instances running concurrently do not publish the same row twice under normal operation (document the residual at-least-once cases).
- Test: same `Idempotency-Key` twice creates one order.
- ADR written for any design choice that deviated from `messaging.md`.

**Out of scope**: Kafka, Debezium/CDC, `LISTEN/NOTIFY` optimization.

---

## Phase 3: Payments + full saga with compensation

**Scope**
- **Payments** service: `Payment` aggregate, `IPaymentGateway` port, `FakePaymentGateway` adapter with configurable outcomes (success, decline, timeout), resilience policy around the gateway call.
- Events: `PaymentCompletedV1`, `PaymentFailedV1`, `OrderConfirmedV1`, `OrderCancelledV1`, `StockReleasedV1`.
- Orders reacts to payment events. Inventory commits the reservation on `OrderConfirmed` and **releases stock** on `OrderCancelled` (compensation).
- Payments is idempotent per order (never charge the same order twice, even on duplicate `StockReserved`).

**Done when**
- E2E test, happy path: order → `Confirmed`, stock decremented.
- E2E test, declined payment: order → `Cancelled`, stock released and available again.
- E2E test: duplicate `StockReserved` does not produce two payments.
- All new consumers use the Inbox and all publishers use the Outbox.

**Out of scope**: real payment provider (a Stripe sandbox adapter is an optional later extra and needs approval and an ADR).

---

## Phase 4: Notifications

**Scope**
- **Notifications** service consuming `OrderConfirmed` and `OrderCancelled`.
- Ports `IEmailSender` and `IPushSender`. Email adapter sends via SMTP to **Mailpit**; push adapter is a fake/console implementation.
- Message templates, `NotificationLog` for dedupe and audit.
- Notification failures never affect the order (they are retried independently, then dead-lettered).

**Done when**
- Integration test: confirmed order produces exactly one email visible in Mailpit (via its API) even if the event is delivered twice.
- Cancelled order produces one cancellation email with the reason.
- A failing email sender triggers retries and ends in the DLQ without impacting Orders.

**Out of scope**: real push provider, HTML design polish, user preference management.

---

## Phase 5: Observability and test suite

**Scope**
- `Ordo.BuildingBlocks.Observability`: OpenTelemetry traces and metrics for HTTP, EF Core and messaging, OTLP export.
- Trace context propagated through outbox → broker → consumer, `correlation_id` in log scopes.
- Health checks (`/health/live`, `/health/ready`) on every service.
- Jaeger (or Seq/OTLP collector) added to `deploy/`.
- Contract tests for every event version.
- Consolidated E2E suite for the full saga.

**Done when**
- A single order produces **one connected trace** spanning Orders → Inventory → Payments → Orders → Notifications (show a screenshot or exported trace in the report).
- Metrics for outbox pending count, consumer processing time and DLQ depth are exposed.
- Changing a property of an existing event version breaks a contract test.

**Out of scope**: production dashboards and alerting.

---

## Phase 6: Kafka adapter

**Scope**
- `Ordo.BuildingBlocks.Messaging.Kafka` implementing `IBrokerPublisher` and the consumer host per `messaging.md` section 9.
- Kafka added to `deploy/` under a compose profile.
- Migrate **one** service (suggested: Inventory) to Kafka via configuration, while the others remain on RabbitMQ, or run both side by side if a bridge is required (propose the approach and write an ADR first).

**Done when**
- The migrated service runs the full saga with **zero changes** in its `Domain` and `Application` projects (verify with `git diff` against the pre-phase commit).
- Inbox idempotency is still exercised by tests under Kafka.
- An ADR documents what leaked (if anything) and what the abstraction cost or saved.

**Out of scope**: removing RabbitMQ, Kafka Streams, schema registry (may be a later extra).

---

## Extras (pick later, each needs a plan and an ADR)

- API gateway with YARP.
- .NET Aspire for local orchestration.
- Event Sourcing for `Payment`.
- Orchestrated saga spike (Wolverine or MassTransit state machine) **in an isolated branch**, plus an ADR comparing it to the choreographed version.
- Replace the hand-built outbox in one service with a library's outbox in an isolated branch and write the comparison ADR.
- CDC with Debezium instead of polling.
- `LISTEN/NOTIFY` to wake the relay and reduce latency.
- DLQ replay tool.
- Stripe sandbox payment adapter.
