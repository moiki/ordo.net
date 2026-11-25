# Ordo.Inventory.Api
**Purpose:** Composition root and HTTP inbound adapter for Inventory.
**Layer:** Api.
**May reference:** `Ordo.Inventory.Application`, `Ordo.Inventory.Infrastructure`, `Ordo.Contracts`, `BuildingBlocks.*` and ASP.NET Core, per `docs/architecture.md` §4.
**Contains:** `Program.cs` with `/health/live`, `/health/ready`, `PUT /inventory/stock/{productId}`, OpenAPI, direct handler registration and the `orders.order-created.v1` consumer on `inventory.on-order-created`.
**Does NOT contain:** Business logic or another service's code (see [AGENTS.md](../../../AGENTS.md) §6).
**Configuration:** `ConnectionStrings:Inventory` and `ConnectionStrings:RabbitMq` must come from environment variables or user secrets. Dev credentials stay in `deploy/docker-compose.yml`; nothing is committed in `appsettings*.json`.
**Dev-only:** `PUT /inventory/stock/{productId}` has **no authentication in Phase 1**; expose it only on a local development host.
**How to test:** `dotnet test Ordo.slnx -c Release --no-build`; end-to-end tests exercise this host in Phase 1.
**Status:** Phase 1 endpoint and consumer wired. No TODO(phase-N) here.
