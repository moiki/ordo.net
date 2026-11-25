# 0005. CQRS with explicit handlers and decorators (no mediator library)

- Status: Accepted
- Date: 2026-11-24

## Context
Ordo separates commands from queries (CQRS at the use-case level) inside each hexagonal service. A common way to do this in .NET is a mediator library that dispatches requests to handlers in memory.

Licensing context:
- MediatR 13 and later is released under a dual license (RPL-1.5 or commercial), with a free Community edition limited by company size and use, which requires a license key.
- MediatR 12.5.0 is the last release under the previous permissive license, but it is frozen and will not receive future patches.
- `martinothamar/Mediator` is a source-generator-based alternative with a near-identical API, reported as MIT licensed and actively maintained.

Technical context:
- CQRS does not require a mediator. A mediator is only an in-process dispatch mechanism.
- In a hexagonal service, endpoints and message consumers are already adapters that can call a handler interface directly.
- Message consumers already need a per-type handler registry for integration events.
- The project is a learning project aimed at senior-level skills; building the cross-cutting pipeline explicitly is part of the value.

Alternatives considered:
1. MediatR 13+ (license and cost).
2. MediatR 12.5.0 (frozen, no future security patches).
3. `martinothamar/Mediator` (viable, but adds a dependency for little gain at this stage).
4. A hand-written mediator (~40 lines) that dispatches by request type.
5. No mediator: direct handler injection plus decorators.
6. Messaging frameworks (Wolverine, MassTransit): out of scope, they are deliberately excluded from core code.

## Decision
Use **option 5**. Each use case is a class implementing a small explicit interface (`ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`). Endpoints and consumers inject and call the handler interface directly.

Cross-cutting concerns (logging, tracing and metrics, validation, later possibly idempotency) are implemented as **decorators** with the same interface. Decorator registration is a small hand-written helper in the composition root. Scrutor requires approval before use.

`SaveChangesAsync` remains explicit inside handlers during Phases 1 and 2 to keep the atomicity with the outbox visible.

## Consequences
- Positive: no third-party licensing risk for the dispatch layer; dependencies stay explicit and navigable; the pipeline is understood end to end; handler interfaces stay stable.
- Negative: decorator registration with open generics in the built-in DI container needs a helper; slightly more boilerplate than a mediator with pipeline behaviors; in-process notifications (publish to many handlers) are not provided.
- Revisit: if decorator boilerplate becomes a real burden (expected checkpoint: Phase 2 or 3) or in-process notifications are needed, evaluate `martinothamar/Mediator` through a new ADR. Because handlers are plain classes behind one interface, such a migration would be mostly mechanical.
