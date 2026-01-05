# Copilot/Codex Instructions — Radiology Coding AI

## Non-negotiable rules
1) Do NOT change public contracts in docs/SPEC_RadiologyCodingAI.md without explicitly updating the spec first.
2) Do NOT invent ICD codes or descriptions. All codes must come from Terminology API results.
3) All SQL must be parameterized (FromSqlRaw with NpgsqlParameter). No string interpolation.
4) Keep behavior deterministic (no randomization).
5) Secondary ICDs are suggestions only (never auto-finalize) in MVP.
6) Respect safety gates: missing indication OR pack global-only fallback OR low documentation completeness => block auto primary ICD.

## Repo structure
- src/Services/Extraction.Worker
- src/Services/Coding.Worker
- src/Services/Terminology.Api
- docs/

## Tech stack
- .NET 8
- EF Core + Npgsql
- PostgreSQL + pgvector + pg_trgm + unaccent
- Minimal API for Terminology.Api
- BackgroundService workers for workers

## Coding standards
- Use DI everywhere
- Use async/await properly
- Add basic validation + structured logging
- Separate "Contracts" from "Services" from "Data"
- Prefer small classes with single responsibility
- Add cancellation token to async operations

## Testing
- Add minimal unit tests for:
  - Coding policy eligibility rules
  - SafetyGate logic
- Keep tests deterministic and fast

## Deliverables per task
Follow docs/TASKS.md in order. Each task must:
- Build successfully
- Have compile-clean code
- Include brief comments where logic is non-obvious
