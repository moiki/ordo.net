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

- `docs/roadmap.md`
- `docs/architecture.md`
- `docs/messaging.md` (to be added when messaging phase starts)
- `docs/conventions.md`
- `docs/adr/`
