# Ordo.Orders.Api
**Purpose:** Composition root and HTTP inbound adapter for Orders.
**Layer:** Api.
**May reference:** `Ordo.Orders.Application`, `Ordo.Orders.Infrastructure`, `Ordo.Contracts`, `BuildingBlocks.*` and ASP.NET Core, per `docs/architecture.md` §4.
**Contains:** `Program.cs` with `POST /orders`, `GET /orders/{id}`, `/health/live`, `/health/ready` (database check), OpenAPI, direct handler registration, and the two stock-result consumers (`orders.on-stock-reserved`, `orders.on-stock-rejected`).
**Does NOT contain:** Business logic or another service's code (see [AGENTS.md](../../../AGENTS.md) §6).
**Configuration:** `ConnectionStrings:Orders` and `ConnectionStrings:RabbitMq` must come from environment variables or user secrets. Dev credentials stay in `deploy/docker-compose.yml`; nothing is committed in `appsettings*.json`. Example local names: `orders`, exchange `ordo.events`.
**How to test:** `dotnet test Ordo.slnx -c Release --no-build`; end-to-end tests exercise this host in Phase 1.
**Status:** Phase 1 endpoints and consumers wired. No TODO(phase-N) here.
