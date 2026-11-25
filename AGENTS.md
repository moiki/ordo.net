# AGENTS.md — Ordo

Ordo is a checkout and order-orchestration platform for small and medium businesses. A merchant registers products or services, a customer pays, and the platform handles stock, order confirmation and notifications.

It is built as an **Event-Driven Architecture (EDA)** in **.NET** with **hexagonal architecture** per service, **RabbitMQ** as the first broker (designed so Kafka can replace it later), and **PostgreSQL** (one database per service).

This is also a **learning project for a developer growing into a senior role**. Correctness, explicit trade-offs and understanding matter more than speed. Do not hide complexity behind libraries that the project has deliberately chosen to build by hand.

> Read this file completely before every task. Detailed rules live in `docs/`. If a rule here conflicts with a doc, **this file wins**; the conflict must be reported and corrected in the same change.

## 1. Anti-hallucination protocol (highest priority)

1. **Never invent** package names, package versions, API signatures, CLI flags, config keys, or file paths. Verify first.
   - Packages and versions: use `dotnet package search <name>` or inspect NuGet. When adding a package, use `dotnet add package <name>` (Central Package Management writes the version into `Directory.Packages.props`). Never type a version from memory.
   - APIs: check official docs or inspect the installed package. If you cannot verify, say so explicitly.
2. **If you are unsure, stop and ask.** Write `QUESTION:` followed by the doubt and your proposed default. Do not guess and continue.
3. **Do not add dependencies** that are not in the allowed list in `docs/conventions.md` without approval.
4. **Never claim something works without running it.** "It builds" or "tests pass" requires you to have run the command in this session and read the result. If you could not run it (no Docker, no network), say so.
5. **No fake implementations.** Anything unfinished must be `throw new NotImplementedException("TODO(phase-N): <what>")` and be listed in your final report. Never leave code that looks complete but is not.
6. **Do not change architecture rules, contracts or tests to make something compile or pass.** If a rule blocks you, explain why and propose an ADR (see `docs/conventions.md`).
7. **Stay in scope.** Work on the current phase of `docs/roadmap.md` only. Do not "improve" unrelated code, rename things broadly, or start the next phase.
8. **Prefer small diffs.** One logical change per commit, using Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`).

## 2. Workflow for every task

1. Read the relevant docs: `docs/roadmap.md` (current phase), then `architecture.md`, `messaging.md` or `conventions.md` as needed.
2. **Propose a short plan** (files to create or change, order, tests) and **wait for approval** before writing code for any non-trivial task or at the start of each phase.
3. Implement in vertical slices (domain → application → infrastructure → API → tests).
4. Run the checks in section 4.
5. End with a **report**:
   - What changed (files, behavior)
   - Commands run and their results
   - Deviations from the docs and why
   - Open questions and `TODO(phase-N)` items

## 3. Stack

| Concern | Choice |
|---|---|
| Runtime | **.NET 10 (LTS), target framework `net10.0`** (ADR 0004). Run `dotnet --version` to confirm a 10.x SDK is installed; if not, stop and tell the human. Do **not** move to .NET 11 without a new ADR |
| Solution format | `.slnx` (single `Ordo.slnx` at repo root) |
| Language | C# with `Nullable` enabled |
| Web | ASP.NET Core Minimal APIs |
| Use cases (CQRS) | Plain `ICommandHandler` / `IQueryHandler` classes injected directly, cross-cutting concerns via **decorators** (ADR 0005). **No MediatR or any mediator library** |
| Persistence | PostgreSQL + EF Core (Npgsql provider), one database per service |
| Broker (first) | RabbitMQ using the official `RabbitMQ.Client` **7.x async API** (`IConnection`, `IChannel`). The `IModel` API is legacy; do not use it |
| Broker (later) | Kafka adapter (Phase 6). Nothing outside the adapter may depend on RabbitMQ concepts |
| Messaging reliability | Hand-built Outbox and Inbox (Phase 2). **Do not use** MassTransit, Wolverine or NServiceBus in core code |
| Tests | xUnit, Testcontainers (PostgreSQL, RabbitMQ), architecture tests |
| Local infra | Docker Compose in `deploy/` |

## 4. Commands

Run from the repo root. If a command does not behave as expected with `.slnx` on the installed SDK, ask instead of working around it.

```bash
dotnet restore Ordo.slnx
dotnet build Ordo.slnx -c Release --no-restore
dotnet test Ordo.slnx -c Release --no-build
dotnet format Ordo.slnx --verify-no-changes

docker compose -f deploy/docker-compose.yml up -d     # local infra
docker compose -f deploy/docker-compose.yml down -v   # reset local infra
```

Integration tests need Docker running (Testcontainers). A task is **not done** until build, tests and format check pass, or you have clearly reported why they could not run.

## 5. Repository layout

```
ordo/
├── AGENTS.md
├── Ordo.slnx
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── .editorconfig
├── src/
│   ├── BuildingBlocks/          # technical infrastructure only, NO business logic
│   │   ├── Ordo.BuildingBlocks.Messaging.Abstractions/
│   │   ├── Ordo.BuildingBlocks.Messaging.RabbitMq/    (Phase 1 — not yet created)
│   │   └── Ordo.BuildingBlocks.Messaging.EfCore/      (Phase 2 — not yet created; outbox + inbox)
│   ├── Contracts/
│   │   └── Ordo.Contracts/      # integration events only
│   ├── Orders/      (Ordo.Orders.Domain | .Application | .Infrastructure | .Api)
│   └── Inventory/   (same four projects)
├── tests/
│   └── Ordo.ArchitectureTests/
├── deploy/                      # docker-compose, broker config
└── docs/                        # architecture, messaging, conventions, roadmap, adr/
```

Namespaces mirror project names: `Ordo.Orders.Domain`, `Ordo.Contracts`, `Ordo.BuildingBlocks.Messaging.Abstractions`.

## 6. Hard architectural rules (summary, details in `docs/architecture.md`)

- **Domain** references nothing: no project references, no NuGet packages (BCL only).
- **Application** references Domain, `Ordo.Contracts` and `Messaging.Abstractions` only. No EF Core, no RabbitMQ, no ASP.NET.
- `BuildingBlocks.Messaging.Abstractions` may reference `Ordo.Contracts` only (it needs `IIntegrationEvent`); no packages and no other project references.
- **Infrastructure** implements the ports defined in Application. It is the only place that knows EF Core, RabbitMQ, SMTP, etc.
- **Api** is the composition root and the HTTP inbound adapter. No business logic.
- **Services never reference each other.** They share only `Ordo.Contracts`. No shared domain code, no shared databases, no synchronous service-to-service HTTP calls.
- `BuildingBlocks` must stay generic and technical. If it starts to contain domain concepts, stop and ask.
- Architecture tests in `tests/Ordo.ArchitectureTests` enforce these rules and must never be weakened.
- Endpoints and message consumers call handlers **directly** through their interfaces. There is no in-process mediator/bus. Logging, validation and similar concerns are **decorators** around handlers (see `docs/architecture.md`).

## 7. Hard messaging rules (summary, details in `docs/messaging.md`)

- Delivery is **at-least-once**. Every consumer must be idempotent (Inbox).
- State change and event publication must be atomic (Outbox) from Phase 2 onward.
- Integration events are immutable `record`s in `Ordo.Contracts`, versioned by a stable type string such as `orders.order-created.v1`. Never put .NET type names on the wire.
- Never break a published event version. Breaking change means a new `vN`.
- Application code must not depend on global ordering, broker redelivery counters, per-message TTLs, priorities or request/reply over the broker. These do not port to Kafka.

## 8. Things you must never do

- Commit secrets, connection strings with real credentials, or `.env` files. Local dev credentials live only in `deploy/` and are clearly dev-only.
- Use `async void`, `.Result`, `.Wait()`, `Thread.Sleep` in production code, or swallow exceptions silently.
- Share an EF `DbContext` or database across services.
- Reference `DateTime.UtcNow` directly in domain or application code. Use `TimeProvider`.
- Disable analyzers or warnings, or add `#pragma warning disable`, without approval.
- Delete or rewrite existing ADRs. Supersede them with a new one.
- Generate large amounts of code in one step without a plan the human has approved.

## 9. Documentation index

| File | Read it when |
|---|---|
| `docs/roadmap.md` | Always, to know the current phase and its "done" criteria |
| `docs/architecture.md` | Creating or changing projects, references, aggregates, use cases |
| `docs/messaging.md` | Touching events, RabbitMQ, outbox, inbox, retries, tracing |
| `docs/conventions.md` | Writing code, adding packages, writing tests, writing ADRs |
| `docs/adr/` | Before reversing or questioning an earlier decision |

## 10. Documentation upkeep

Every new project ships with a `README.md` using the template in `docs/conventions.md`. Any change to a project's responsibility, references or status updates its `README.md` in the same commit.
