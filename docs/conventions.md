# Conventions

## 1. Solution-wide setup

- Single solution file: `Ordo.slnx` at the repo root.
- `global.json` pins a **.NET 10 SDK** (ADR 0004). Run `dotnet --version` and tell the human what is installed. If it is not a 10.x SDK, stop and ask. Do not target .NET 11 without a new ADR.
- `Directory.Build.props` sets for all projects:
  - Target framework `net10.0` (ADR 0004), `Nullable` enabled, `ImplicitUsings` enabled
  - `TreatWarningsAsErrors` = true, `AnalysisLevel` latest, `EnforceCodeStyleInBuild` = true
  - `Deterministic` = true, `InvariantGlobalization` where applicable
  - Shared metadata (company/product `Ordo`)
- `Directory.Packages.props` uses **Central Package Management** (`ManagePackageVersionsCentrally`). Project files contain `<PackageReference Include="X" />` with **no version**.
- `.editorconfig` defines style and naming rules and is enforced by `dotnet format --verify-no-changes` and the build.

## 2. C# style

- File-scoped namespaces. One top-level type per file; the file name equals the type name.
- `record` for DTOs, commands, queries, contracts and value objects (use `record struct` only with a reason).
- `sealed` by default for classes that are not designed for inheritance.
- Prefer constructor injection with **primary constructors** where it reads clearly.
- Async everywhere on I/O. Method names end in `Async`. Every async method that does I/O accepts a `CancellationToken` and passes it down.
- No `async void` (except event handlers where unavoidable), no `.Result`, no `.Wait()`, no `Task.Run` to fake async.
- Time comes from `TimeProvider`. Ids for new messages and aggregates use `Guid.CreateVersion7()` unless a different strategy is agreed.
- No static mutable state. No service locator. No `IServiceProvider` injected into domain/application classes.
- Do not catch `Exception` unless you rethrow, translate, or are at a process boundary (consumer loop, hosted service loop), and always log with context.
- Public API of Domain/Application is small; make things `internal` by default.
- Comments explain **why**, not what. Do not leave commented-out code.

### Naming
- Commands: `CreateOrderCommand`, handlers: `CreateOrderHandler`.
- Queries: `GetOrderQuery`, `GetOrderHandler`.
- Domain events: past tense, `OrderPlaced` (in the Domain project).
- Integration events: past tense + version, `OrderCreatedV1` (in `Ordo.Contracts`).
- Ports: `I` + role, `IOrderRepository`, `IPaymentGateway`.
- Adapters: technology + role, `EfOrderRepository`, `RabbitMqBrokerPublisher`, `FakePaymentGateway`.
- Test classes: `<TypeUnderTest>Tests`; test methods: `Method_Scenario_ExpectedResult`.

## 3. Persistence conventions (EF Core + PostgreSQL)

- One `DbContext` per service, in that service's Infrastructure project.
- Use `IEntityTypeConfiguration<T>` classes. No data annotations on domain entities.
- snake_case table and column names (agree on the mechanism, e.g. a naming-convention package or explicit mapping, before adding a package).
- Dedicated schema or database per service; migrations live in Infrastructure and are applied explicitly (not silently on production startup; a dev-only auto-migrate flag is acceptable).
- No lazy loading. Load what you need explicitly. Repositories return aggregates, not `IQueryable`.
- Value objects mapped as owned types or converters.
- Use `AsNoTracking` for read-only queries.

## 4. Error handling and logging

- Expected business failures use a small `Result`/`Error` type defined in the service's Application layer (do not add a Result library without approval).
- API errors use `ProblemDetails`.
- Logging uses `Microsoft.Extensions.Logging` with **structured** messages (`logger.LogInformation("Order {OrderId} confirmed", orderId)`), never string interpolation in log calls. Prefer source-generated `LoggerMessage` for hot paths.
- Never log secrets or full personal data.

## 5. Configuration

- Options pattern (`IOptions<T>`) with validation on start (`ValidateOnStart`).
- No secrets in the repo. Local dev values live in `deploy/` files clearly marked dev-only. Production configuration is out of scope until a deployment phase is planned.

## 6. Dependency policy

Every NuGet package is a liability. Add a package only if it is in the **allowed** list or approved by the human. Before adding any package, check its **license and maintenance status**.

### Allowed (verify the exact current package name with `dotnet package search`)

- Microsoft: ASP.NET Core, `Microsoft.EntityFrameworkCore` and design/tools, `Microsoft.Extensions.*` (including Hosting, Options, Logging, Diagnostics.HealthChecks, Resilience)
- Npgsql EF Core provider for PostgreSQL, Npgsql
- `RabbitMQ.Client` (7.x)
- OpenTelemetry packages (traces, metrics, OTLP exporter, ASP.NET Core and HTTP instrumentation)
- Tests: xUnit, `Microsoft.NET.Test.Sdk`, Shouldly (assertions), NSubstitute (mocking), Testcontainers (PostgreSQL, RabbitMQ), `Microsoft.AspNetCore.Mvc.Testing`, `NetArchTest.Rules`
- Phase 6 only: a Kafka client (expected: `Confluent.Kafka`, confirm before use)

### Ask first
Anything not listed above, including Serilog, Dapper, FluentValidation, Bogus, Polly directly (prefer `Microsoft.Extensions.Resilience`), MailKit (needed in Phase 4, ask then), Stripe SDK, and **Scrutor** (decorator registration; first try a small hand-written helper in the composition root, see `architecture.md`).

### Do not use in core code
- **MassTransit, Wolverine, NServiceBus, Rebus, CAP**: the Outbox/Inbox/retry are deliberately hand-built. Library comparisons happen only in an isolated spike branch with its own ADR (see roadmap extras).
- **MediatR (any version) and AutoMapper**: use plain handlers with decorators (ADR 0005) and explicit mapping. MediatR 13+ is under a dual/commercial license and 12.5 is frozen without future patches. The only mediator candidate ever to be reconsidered is `martinothamar/Mediator`, and only through a new ADR.
- **FluentAssertions**: recent major versions use a commercial license. Use Shouldly.
- **Moq**: use NSubstitute.

## 7. Testing

| Level | Scope | Tools |
|---|---|---|
| Unit | Domain behavior and Application handlers with fake ports | xUnit, Shouldly, NSubstitute |
| Integration | One service against real PostgreSQL and RabbitMQ | Testcontainers, `WebApplicationFactory` |
| Architecture | Dependency and naming rules | `NetArchTest.Rules` |
| Contract | JSON shape of each event version | xUnit snapshot-style assertions (Phase 5) |
| End-to-end | Full saga across services | Testcontainers, orchestrated in test code (Phase 5) |

Rules:
- Domain logic is tested **without** mocks of infrastructure. Test invariants and state transitions directly.
- Every bug fix starts with a failing test.
- Tests are deterministic: fixed `TimeProvider`, no sleeps for synchronization (poll with timeout), no shared mutable state between tests.
- Testcontainers fixtures are shared per test class/collection to keep the suite fast.
- A failing or flaky test is never "fixed" by deleting it, skipping it, or adding retries. Find the cause.
- Do not assert on log text. Assert on behavior and state.

## 8. Docker and local infrastructure (`deploy/`)

- `deploy/docker-compose.yml` provides: PostgreSQL (**version 16 or later**, so a future move to the Npgsql EF Core 11 provider, which assumes 16+ by default, needs no database change), RabbitMQ (with the management UI), and **Mailpit** (local SMTP sink for Phase 4). Add Jaeger/Seq or an OTLP collector in Phase 5, Kafka under a compose **profile** in Phase 6.
- Pin explicit image tags (verify they exist; do not use `latest`).
- Every service has a healthcheck so `docker compose up --wait` works.
- Dev credentials are obviously dev-only (e.g. `ordo_dev`), never reused elsewhere.

## 9. Git and CI

- Conventional Commits. Small, focused commits. Branch per phase or feature.
- CI (GitHub Actions) on every push and PR: restore, build (Release), test (including Testcontainers-based tests if the runner supports Docker), format check.
- Do not add steps that deploy anywhere until a deployment phase is planned.

## 10. Architecture Decision Records (ADRs)

Location: `docs/adr/NNNN-short-title.md`, numbered sequentially. ADRs are **append-only**: to reverse a decision, write a new ADR that supersedes the old one.

Write an ADR when you: choose between meaningful alternatives, add a non-allowed dependency, change a rule in these docs, or discover that an abstraction leaked.

Template:

```markdown
# NNNN. Title

- Status: Proposed | Accepted | Superseded by NNNN
- Date: YYYY-MM-DD

## Context
What problem or force requires a decision? Constraints, alternatives considered.

## Decision
What we decided, in one or two clear paragraphs.

## Consequences
Positive, negative, and what becomes harder. Include what we would need to revisit and when.
```

The agent may **draft** ADRs, but the human decides and owns the "Accepted" status. Never mark an ADR Accepted without the human's confirmation.

## 11. Project READMEs

Every project under `src/` and `tests/` has a short `README.md` (about 15 lines, in English). A new project is created together with its README, and any change to its responsibility, references or status updates the README in the **same commit**. An architecture test fails if a project directory has no README.

Template:

```markdown
# <Full project name>
**Purpose:** one sentence.
**Layer:** Domain | Application | Infrastructure | Api | Contracts | BuildingBlocks | Tests.
**May reference:** per the matrix in docs/architecture.md (only what it really references today).
**Contains:** main folders or types that exist TODAY.
**Does NOT contain:** what must not live here, per docs/architecture.md.
**How to test:** a concrete command, or "no tests yet".
**Status:** what is implemented and pending TODO(phase-N) items.
```

Content rules:
- Describe only what exists in the code now. Never document future functionality as if it were implemented.
- Do not invent commands, paths or APIs; verify them.
- Do not copy long rules from `docs/`; link to them with a relative path.
- For test projects, state which rules or behaviors they cover.
- If a project is empty, say so under Status.
- The root `README.md` has a table linking to every project README.
