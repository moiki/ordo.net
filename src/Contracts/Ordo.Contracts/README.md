# Ordo.Contracts
**Purpose:** Holds the shared integration-event contract used across services.
**Layer:** Contracts.
**May reference:** Nothing outside the BCL (no ProjectReference/PackageReference today).
**Contains:** `IIntegrationEvent.cs` (`EventId`, `OccurredAtUtc`).
**Does NOT contain:** Event implementations, service dependencies or .NET type names on the wire (see [AGENTS.md](../../AGENTS.md) §7).
**How to test:** No project-specific tests yet; `dotnet test Ordo.slnx -c Release --no-build` from the repo root.
**Status:** Interface only; no concrete events exist yet. No TODO(phase-N) here.
