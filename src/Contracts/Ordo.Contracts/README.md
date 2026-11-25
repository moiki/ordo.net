# Ordo.Contracts
**Purpose:** Holds the shared integration-event contract used across services.
**Layer:** Contracts.
**May reference:** Nothing outside the BCL (no ProjectReference/PackageReference today).
**Contains:** `IIntegrationEvent.cs` (`EventId`, `OccurredAtUtc`, `EventType`, `PartitionKey`, ADR 0006) and the Phase 1 event records in `Orders/` and `Inventory/`.
**Does NOT contain:** Service dependencies or .NET type names on the wire (see [AGENTS.md](../../AGENTS.md) §7). Event metadata is `[JsonIgnore]` and travels in broker headers.
**How to test:** `dotnet test Ordo.slnx -c Release --no-build` from the repo root.
**Status:** Phase 1 events available. No TODO(phase-N) here.
