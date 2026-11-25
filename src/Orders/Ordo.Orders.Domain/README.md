# Ordo.Orders.Domain
**Purpose:** Owns the Orders service domain model and its rules.
**Layer:** Domain.
**May reference:** Nothing outside the BCL (no ProjectReference/PackageReference today).
**Contains:** `AssemblyMarker.cs`; no aggregates or value objects yet.
**Does NOT contain:** EF Core, RabbitMQ, ASP.NET or other services (see [AGENTS.md](../../../AGENTS.md) §6).
**How to test:** No tests yet; `dotnet test Ordo.slnx -c Release --no-build` from the repo root checks the dependency rules.
**Status:** Empty skeleton with assembly marker only. No TODO(phase-N) here.
