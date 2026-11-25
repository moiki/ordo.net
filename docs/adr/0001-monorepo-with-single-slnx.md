# 0001. Monorepo with a single .slnx

- Status: Proposed
- Date: 2026-11-24

## Context

Ordo is a learning project that will evolve through multiple services, shared building blocks, contracts, and architecture tests. The team needs a repository layout that makes dependency rules explicit, keeps cross-service contracts visible, and supports phased delivery without premature distribution complexity.

Alternatives considered:
- Multiple repositories per service and shared packages.
- Monorepo with multiple solutions.
- Monorepo with a single `.slnx` solution at the repository root.

## Decision

Use a single monorepo with one `Ordo.slnx` at the root. Services, contracts, building blocks, tests, deployment assets, and documentation live in the same repository. Project references and architecture tests enforce boundaries inside the single solution.

## Consequences

- Positive: simple local setup, atomic refactors across contracts and services, easier architecture enforcement, and clear visibility for learning.
- Negative: repository size and CI scope can grow; discipline is required to avoid accidental coupling.
- Revisit: if independent deployment/release cadence or team ownership requires repository split, supersede with a new ADR.
