# Ordo.ArchitectureTests
**Purpose:** Enforces the architecture rules stated in [AGENTS.md](../../AGENTS.md) §3, §6 and §7.
**Layer:** Tests.
**May reference:** All current `src/` projects and the packages in `Ordo.ArchitectureTests.csproj` (xUnit, Shouldly, NetArchTest.Rules, Microsoft.NET.Test.Sdk).
**Contains:** `ArchitectureRulesTests.cs` (domain isolation, Application framework limits, no cross-service refs, Contracts shape, event naming, technical building blocks), `ProjectReferenceMatrixTests.cs` (csproj dependency matrix; `Messaging.Abstractions` → `Ordo.Contracts` is allowed per `docs/architecture.md` §4) and `ProjectReadmePresenceTests.cs` (one README per project directory).
**Does NOT contain:** Business logic, production adapters or assertions that weaken the rules (see [AGENTS.md](../../AGENTS.md) §6).
**How to test:** `dotnet test Ordo.slnx -c Release --no-build` from the repo root (8 tests today).
**Status:** 8 passing tests; the README-presence rule applies to every project directory under `src/` and `tests/`. No TODO(phase-N) here.
