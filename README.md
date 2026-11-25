# Ordo

Ordo is a checkout and order-orchestration learning platform for small and medium businesses.

## Phase 0 foundations

- .NET 10 (`net10.0`) solution with Central Package Management
- Hexagonal service skeletons for Orders and Inventory
- Shared contracts and messaging building-block abstractions
- Architecture tests enforcing dependency rules
- Local infrastructure via Docker Compose (PostgreSQL, RabbitMQ, Mailpit)

## Commands

```bash
dotnet restore Ordo.slnx
dotnet build Ordo.slnx -c Release --no-restore
dotnet test Ordo.slnx -c Release --no-build
dotnet format Ordo.slnx --verify-no-changes

docker compose -f deploy/docker-compose.yml up -d --wait
docker compose -f deploy/docker-compose.yml down -v
```

## Docs

`docs/` is the detailed source of truth; `AGENTS.md` §0 wins on conflict.

- `docs/roadmap.md`
- `docs/architecture.md`
- `docs/messaging.md`
- `docs/conventions.md`
- `docs/adr/`

## Project READMEs

| Project | Layer | README |
|---|---|---|
| Ordo.Contracts | Contracts | [README](src/Contracts/Ordo.Contracts/README.md) |
| Ordo.BuildingBlocks.Messaging.Abstractions | BuildingBlocks | [README](src/BuildingBlocks/Ordo.BuildingBlocks.Messaging.Abstractions/README.md) |
| Ordo.Orders.Domain | Domain | [README](src/Orders/Ordo.Orders.Domain/README.md) |
| Ordo.Orders.Application | Application | [README](src/Orders/Ordo.Orders.Application/README.md) |
| Ordo.Orders.Infrastructure | Infrastructure | [README](src/Orders/Ordo.Orders.Infrastructure/README.md) |
| Ordo.Orders.Api | Api | [README](src/Orders/Ordo.Orders.Api/README.md) |
| Ordo.Inventory.Domain | Domain | [README](src/Inventory/Ordo.Inventory.Domain/README.md) |
| Ordo.Inventory.Application | Application | [README](src/Inventory/Ordo.Inventory.Application/README.md) |
| Ordo.Inventory.Infrastructure | Infrastructure | [README](src/Inventory/Ordo.Inventory.Infrastructure/README.md) |
| Ordo.Inventory.Api | Api | [README](src/Inventory/Ordo.Inventory.Api/README.md) |
| Ordo.ArchitectureTests | Tests | [README](tests/Ordo.ArchitectureTests/README.md) |

