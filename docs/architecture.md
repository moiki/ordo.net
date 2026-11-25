# Architecture

## 1. Style

- **Event-Driven Architecture** across services: services collaborate by publishing and reacting to integration events. No synchronous service-to-service calls.
- **Choreographed saga** for the checkout flow (no central orchestrator in the core phases).
- **Hexagonal architecture** (ports and adapters) inside each service.
- **Database per service.** One PostgreSQL instance may host several databases in local dev, but each service owns its database and no service reads another's.
- Consistency between services is **eventual**. The API reflects this (an order is `Pending` until the saga completes).

## 2. Services and responsibilities

| Service | Owns | Main aggregates |
|---|---|---|
| **Orders** | Order lifecycle | `Order` (with order lines) |
| **Inventory** | Stock and reservations | `StockItem`, `Reservation` |
| **Payments** | Charging the customer | `Payment` |
| **Notifications** | Email and push delivery | `NotificationLog` (for dedupe and audit) |

### Order status machine (Orders)

```
Pending → AwaitingPayment → Confirmed
   │            │
   └────────────┴──────────→ Cancelled
```

- `Pending`: created, waiting for stock reservation.
- `AwaitingPayment`: stock reserved, waiting for payment result.
- `Confirmed`: payment completed.
- `Cancelled`: stock rejected, or payment failed. Carries a reason.

Invalid transitions are rejected **inside the aggregate**, never in the API or handler.

## 3. Layers inside a service

Example for Orders. The same pattern applies to every service.

```
src/Orders/
├── Ordo.Orders.Domain/
├── Ordo.Orders.Application/
├── Ordo.Orders.Infrastructure/
└── Ordo.Orders.Api/
```

### Domain
- Entities, value objects, aggregates, domain events, domain exceptions or errors, domain services.
- **No project references and no NuGet packages.**
- Aggregates enforce invariants and expose behavior, not public setters.
- Aggregates raise **domain events** (internal, in-process facts). They do not reference `Ordo.Contracts`.
- Each service defines its own tiny `Entity` / `AggregateRoot` base types. Small duplication across services is intentional; it keeps Domain free of shared code.
- Money is represented as `long` minor units plus an ISO 4217 currency code in a `Money` value object. Never `double` or `float`.

### Application
- Use cases (one class per command or query), **ports** (interfaces), DTOs, and mapping from domain events to integration events.
- References: Domain, `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions`.
- **No MediatR, no mediator libraries.** A use case is a plain class with an explicit interface, for example:
  ```csharp
  public interface ICommandHandler<in TCommand, TResult>
  {
      Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct);
  }
  ```
- Expected failures (validation, not found, invalid state) return a `Result`/error type. Exceptions are for unexpected failures.
- Ports live in Application, grouped by purpose:
  - Outbound persistence: `IOrderRepository`, `IUnitOfWork`
  - Outbound messaging: `IIntegrationEventPublisher` (from `Messaging.Abstractions`)
  - Outbound external systems: `IPaymentGateway`, `IEmailSender`, `IPushSender`, etc.
  - Inbound message handlers: `IIntegrationEventHandler<TEvent>` implemented in Application (they are use cases triggered by a message).

#### Cross-cutting concerns: decorators, not a mediator (ADR 0005)

- Endpoints and consumers depend on the handler **interface** (`ICommandHandler<,>`, `IQueryHandler<,>`, `IIntegrationEventHandler<>`) and call it directly.
- Logging, tracing/metrics, command validation and similar concerns are implemented as **decorators** that wrap a handler with the same interface:
  ```csharp
  public sealed class LoggingDecorator<TCommand, TResult>(
      ICommandHandler<TCommand, TResult> inner,
      ILogger<LoggingDecorator<TCommand, TResult>> logger)
      : ICommandHandler<TCommand, TResult>
  {
      public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct)
      {
          logger.LogInformation("Handling {Command}", typeof(TCommand).Name);
          return await inner.HandleAsync(command, ct);
      }
  }
  ```
- Generic decorators live in Application (they depend only on abstractions). Their **registration** lives in the composition root (Api/Infrastructure) as a small hand-written helper. Do not add Scrutor without approval.
- `SaveChangesAsync` stays **explicit inside each handler** during Phases 1 and 2, so the atomicity between the business change and the outbox row is visible. A transaction decorator may be proposed later through an ADR.
- Decorator order is explicit and documented in the registration helper (typically: logging/tracing outermost, then validation, then the handler).

### Infrastructure
- Adapters implementing Application ports: EF Core `DbContext`, repositories, outbox/inbox wiring, RabbitMQ consumers registration, SMTP, fake gateways.
- EF Core configuration via `IEntityTypeConfiguration<T>`; migrations live here.
- Provides a `AddOrdersInfrastructure(...)` extension for DI registration.

### Api
- ASP.NET Core Minimal APIs, DI composition root, hosted services (consumers, outbox relay).
- Endpoints translate HTTP to a command or query, call a handler, and map `Result` to HTTP (`ProblemDetails` for errors).
- No business rules. No direct `DbContext` use.
- Consumers and the outbox relay run as `BackgroundService`s inside the Api host for now. Splitting into a separate Worker host is a later, optional decision (needs an ADR).

## 4. Allowed project references

`→` means "may reference". Anything not listed is **forbidden**.

| Project | May reference |
|---|---|
| `*.Domain` | nothing |
| `*.Application` | its own `*.Domain`, `Ordo.Contracts`, `BuildingBlocks.Messaging.Abstractions` |
| `*.Infrastructure` | its own `*.Application`, its own `*.Domain`, `Ordo.Contracts`, any `BuildingBlocks.*` |
| `*.Api` | its own `*.Application`, its own `*.Infrastructure`, `Ordo.Contracts`, `BuildingBlocks.*` |
| `Ordo.Contracts` | nothing (BCL only) |
| `BuildingBlocks.Messaging.Abstractions` | `Ordo.Contracts` only (it needs `IIntegrationEvent`, which lives in `Ordo.Contracts`) |
| `BuildingBlocks.Messaging.RabbitMq` (Phase 1) | `Messaging.Abstractions` |
| `BuildingBlocks.Messaging.EfCore` (Phase 2) | `Messaging.Abstractions` — not yet created |
| Test projects | the projects they test, plus test libraries |

Cross-service rule: **no project of service A may reference any project of service B.** The only shared project is `Ordo.Contracts`.

These rules are enforced by `tests/Ordo.ArchitectureTests`. If a test fails, fix the code, not the test.

## 5. Domain events vs integration events

| | Domain event | Integration event |
|---|---|---|
| Scope | Inside one service | Between services (public contract) |
| Lives in | `*.Domain` | `Ordo.Contracts` |
| Shape | Free to change | Versioned, immutable, backward compatible |
| Transport | In-process | Broker (via outbox) |

Mapping from domain event to integration event is **explicit** and lives in Application (mapper classes or inside the use case). Never expose a domain event or entity on the wire.

## 6. Transaction and publishing flow inside a use case

From Phase 2 onward (outbox):

1. Load aggregate (repository).
2. Execute behavior on the aggregate.
3. Map resulting domain events to integration events and call `IIntegrationEventPublisher.PublishAsync(...)`. The outbox implementation only **adds a row to the current DbContext**.
4. `IUnitOfWork.SaveChangesAsync()` commits the business change and the outbox row **in one transaction**.

In Phase 1 the publisher publishes directly to RabbitMQ after commit. This has a known **dual-write flaw** (crash between commit and publish loses the event). It is intentional, documented, and fixed in Phase 2. Do not try to "fix" it ad hoc earlier.

## 7. Concurrency

- Use optimistic concurrency for aggregates that can be modified concurrently (notably `StockItem`). Use the PostgreSQL `xmin` mechanism as currently documented by the Npgsql EF Core provider (verify in its docs, do not guess).
- A concurrency conflict in a consumer is treated as a **transient** failure (retry), not as a business failure.

## 8. HTTP API conventions

- Minimal APIs, built-in OpenAPI support, `ProblemDetails` for errors.
- `POST /orders` accepts an `Idempotency-Key` header (implemented in Phase 2).
- Health endpoints on every service: `/health/live` (process is up) and `/health/ready` (dependencies reachable).
- Return `202 Accepted` or `201 Created` with the order in `Pending` state. Never block the request waiting for the saga.

## 9. Architecture tests (must exist from Phase 0)

At minimum:
- Domain assemblies have no references to any other `Ordo.*` assembly and no third-party packages.
- Application assemblies do not reference EF Core, RabbitMQ.Client, or ASP.NET Core.
- No service assembly references another service's assemblies.
- `Ordo.Contracts` contains only `record`s/enums and has no dependencies beyond the BCL.
- All integration events implement `IIntegrationEvent` and are named with a version suffix convention (see `messaging.md`).
- The declared `ProjectReference`s of every `.csproj` match the matrix in section 4 (read the `.csproj` files, not only the compiled references, because the compiler drops references that the code does not use).
- Every project directory under `src/` and `tests/` contains a `README.md`.

Use `NetArchTest.Rules`. If it is found to be unmaintained or incompatible with the current SDK, ask before switching to `ArchUnitNET`.
